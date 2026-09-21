using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _paymobService;
        private readonly IOrderService _orderService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymobService paymobService,
            IOrderService orderService,
            IUnitOfWork unitOfWork,
            ILogger<PaymentController> logger)
        {
            _paymobService = paymobService;
            _orderService = orderService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string UserEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? "customer@example.com";

        private string UserName =>
            User.FindFirstValue(ClaimTypes.Name) ?? "Customer";

        private bool IsAdmin =>
            User.IsInRole("Admin");

        // ============================================================
        // INITIATE PAYMENT
        // ============================================================

        [HttpPost("initiate")]
        [Authorize]
        public async Task<IActionResult> Initiate(
            [FromBody] PaymobInitiatePaymentDto dto)
        {
            _logger.LogInformation(
                "Initiating Paymob payment for order {OrderId}",
                dto.OrderId);

            // --------------------------------------------------------
            // 1. Get internal order
            // --------------------------------------------------------

            var order = await _orderService.GetByIdAsync(dto.OrderId);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order {OrderId} not found for payment initiation",
                    dto.OrderId);

                return NotFound(
                    ApiResponse<object>.Fail("Order not found"));
            }

            // --------------------------------------------------------
            // 2. Verify ownership
            // --------------------------------------------------------

            if (order.UserId != UserId && !IsAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to initiate payment for order {OrderId} belonging to {OwnerId}",
                    UserId,
                    dto.OrderId,
                    order.UserId);

                return Forbid();
            }

            // --------------------------------------------------------
            // 3. Verify order status
            // --------------------------------------------------------

            if (order.Status != OrderStatus.PendingPayment)
            {
                _logger.LogWarning(
                    "Order {OrderId} is not in PendingPayment state",
                    dto.OrderId);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Order is not in pending payment state"));
            }

            // --------------------------------------------------------
            // 4. Verify not already paid
            // --------------------------------------------------------

            if (order.IsPaid)
            {
                _logger.LogWarning(
                    "Order {OrderId} is already paid",
                    dto.OrderId);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Order is already paid"));
            }

            try
            {
                var result =
                    await _paymobService.InitiatePaymentAsync(
                        dto,
                        UserEmail,
                        UserName);

                return Ok(
                    ApiResponse<PaymobInitiatePaymentResponseDto>.Ok(
                        result,
                        "Payment initiated"));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to initiate Paymob payment for order {OrderId}",
                    dto.OrderId);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        $"Payment initiation failed: {ex.Message}"));
            }
        }

        // ============================================================
        // PAYMOB CALLBACK / WEBHOOK
        // ============================================================

        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback(
            [FromBody] PaymobCallbackDto dto,
            [FromQuery(Name = "hmac")] string? hmac)
        {
            // --------------------------------------------------------
            // 1. Validate callback body
            // --------------------------------------------------------

            if (dto.Obj.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning(
                    "Paymob callback received with invalid or empty obj");

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Invalid Paymob callback payload"));
            }

            // --------------------------------------------------------
            // 2. Extract Paymob order ID
            // --------------------------------------------------------

            int paymobOrderId = 0;
            bool callbackSuccess = false;

            if (dto.Obj.TryGetProperty(
                    "order",
                    out var orderProp) &&
                orderProp.ValueKind == JsonValueKind.Object)
            {
                if (orderProp.TryGetProperty(
                        "id",
                        out var orderIdProp))
                {
                    if (orderIdProp.ValueKind == JsonValueKind.Number)
                    {
                        paymobOrderId =
                            orderIdProp.GetInt32();
                    }
                }
            }

            // --------------------------------------------------------
            // 3. Extract success
            // --------------------------------------------------------

            if (dto.Obj.TryGetProperty(
                    "success",
                    out var successProp))
            {
                if (successProp.ValueKind == JsonValueKind.True)
                {
                    callbackSuccess = true;
                }
                else if (successProp.ValueKind == JsonValueKind.False)
                {
                    callbackSuccess = false;
                }
            }

            _logger.LogInformation(
                "Paymob callback received. PaymobOrderId={PaymobOrderId}, Success={Success}",
                paymobOrderId,
                callbackSuccess);

            // --------------------------------------------------------
            // 4. Validate Paymob order ID
            // --------------------------------------------------------

            if (paymobOrderId <= 0)
            {
                _logger.LogWarning(
                    "Paymob callback does not contain a valid order ID");

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Paymob order ID is missing"));
            }

            // --------------------------------------------------------
            // 5. HMAC MUST come from query string
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(hmac))
            {
                _logger.LogWarning(
                    "Paymob callback missing HMAC query parameter");

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "HMAC is required"));
            }

            // --------------------------------------------------------
            // 6. Verify HMAC
            // --------------------------------------------------------

            var isValid =
                await _paymobService.VerifyCallbackAsync(
                    dto,
                    hmac);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Paymob callback HMAC verification failed for Paymob order {PaymobOrderId}",
                    paymobOrderId);

                return Unauthorized(
                    ApiResponse<object>.Fail(
                        "Invalid HMAC signature"));
            }

            _logger.LogInformation(
                "Paymob callback HMAC verified successfully for Paymob order {PaymobOrderId}",
                paymobOrderId);

            // --------------------------------------------------------
            // 7. Find payment record
            // --------------------------------------------------------

            var paymentRecord =
                await _unitOfWork.PaymentRecords
                    .GetAllQueryable()
                    .FirstOrDefaultAsync(
                        p => p.PaymobOrderId == paymobOrderId);

            if (paymentRecord == null)
            {
                _logger.LogError(
                    "No payment record found for Paymob order {PaymobOrderId}",
                    paymobOrderId);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Payment record not found"));
            }

            // --------------------------------------------------------
            // 7.5 Verify callback matches the stored payment record
            // --------------------------------------------------------

            var callbackIntegrationId = 0;
            var callbackAmountCents = 0L;
            string? callbackCurrency = null;

            if (dto.Obj.TryGetProperty(
                    "integration_id",
                    out var integrationProp) &&
                integrationProp.ValueKind ==
                    JsonValueKind.Number)
            {
                callbackIntegrationId =
                    integrationProp.GetInt32();
            }

            if (dto.Obj.TryGetProperty(
                    "amount_cents",
                    out var amountProp) &&
                amountProp.ValueKind ==
                    JsonValueKind.Number)
            {
                callbackAmountCents =
                    amountProp.GetInt64();
            }

            if (dto.Obj.TryGetProperty(
                    "currency",
                    out var currencyProp) &&
                currencyProp.ValueKind ==
                    JsonValueKind.String)
            {
                callbackCurrency =
                    currencyProp.GetString();
            }

            // Integration match (records stored before the
            // IntegrationId field existed keep the default 0 and
            // are not rejected by this check).
            if (paymentRecord.IntegrationId != 0 &&
                callbackIntegrationId != 0 &&
                paymentRecord.IntegrationId !=
                    callbackIntegrationId)
            {
                _logger.LogWarning(
                    "Paymob callback integration mismatch. PaymobOrderId={PaymobOrderId}, RecordIntegrationId={RecordIntegrationId}, CallbackIntegrationId={CallbackIntegrationId}",
                    paymobOrderId,
                    paymentRecord.IntegrationId,
                    callbackIntegrationId);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Payment integration mismatch"));
            }

            // Amount verification
            if (callbackAmountCents > 0 &&
                callbackAmountCents !=
                    paymentRecord.AmountCents)
            {
                _logger.LogWarning(
                    "Paymob callback amount mismatch. PaymobOrderId={PaymobOrderId}, RecordAmountCents={RecordAmountCents}, CallbackAmountCents={CallbackAmountCents}",
                    paymobOrderId,
                    paymentRecord.AmountCents,
                    callbackAmountCents);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Payment amount mismatch"));
            }

            // Currency verification
            if (!string.IsNullOrWhiteSpace(
                    callbackCurrency) &&
                !string.Equals(
                    callbackCurrency,
                    paymentRecord.Currency,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Paymob callback currency mismatch. PaymobOrderId={PaymobOrderId}, RecordCurrency={RecordCurrency}, CallbackCurrency={CallbackCurrency}",
                    paymobOrderId,
                    paymentRecord.Currency,
                    callbackCurrency);

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Payment currency mismatch"));
            }

            // --------------------------------------------------------
            // 8. Idempotency
            // --------------------------------------------------------

            if (paymentRecord.IsProcessed)
            {
                _logger.LogInformation(
                    "Paymob callback already processed. PaymobOrderId={PaymobOrderId}, ProcessedAt={ProcessedAt}",
                    paymobOrderId,
                    paymentRecord.ProcessedAt);

                return Ok(
                    ApiResponse<object>.Ok(
                        null!,
                        "Payment already processed"));
            }

            // --------------------------------------------------------
            // 9. Successful payment
            // --------------------------------------------------------

            if (callbackSuccess)
            {
                _logger.LogInformation(
                    "Payment successful. PaymobOrderId={PaymobOrderId}, InternalOrderId={InternalOrderId}",
                    paymobOrderId,
                    paymentRecord.OrderId);

                try
                {
                    // Finalize order:
                    // - mark order as paid
                    // - deduct stock
                    // - update order status
                    await _orderService.FinalizePaymentAsync(
                        paymentRecord.OrderId);

                    // ------------------------------------------------
                    // Mark payment record as processed
                    // ------------------------------------------------

                    paymentRecord.IsProcessed = true;
                    paymentRecord.ProcessedAt =
                        DateTime.UtcNow;

                    // ------------------------------------------------
                    // Save Paymob transaction ID
                    // ------------------------------------------------

                    if (dto.Obj.TryGetProperty(
                            "id",
                            out var transactionIdProp))
                    {
                        if (transactionIdProp.ValueKind ==
                            JsonValueKind.Number)
                        {
                            paymentRecord.TransactionId =
                                transactionIdProp.GetRawText();
                        }
                        else if (transactionIdProp.ValueKind ==
                                 JsonValueKind.String)
                        {
                            paymentRecord.TransactionId =
                                transactionIdProp.GetString();
                        }
                    }

                    _unitOfWork.PaymentRecords.Update(
                        paymentRecord);

                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation(
                        "Payment processed successfully. PaymobOrderId={PaymobOrderId}, InternalOrderId={InternalOrderId}",
                        paymobOrderId,
                        paymentRecord.OrderId);

                    return Ok(
                        ApiResponse<object>.Ok(
                            null!,
                            "Payment processed successfully"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to finalize order {InternalOrderId} after Paymob callback",
                        paymentRecord.OrderId);

                    // IMPORTANT:
                    // Return non-2xx so Paymob can retry the callback.
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        ApiResponse<object>.Fail(
                            "Payment finalization failed"));
                }
            }

            // --------------------------------------------------------
            // 10. Failed payment
            // --------------------------------------------------------

            _logger.LogWarning(
                "Paymob payment failed. PaymobOrderId={PaymobOrderId}, InternalOrderId={InternalOrderId}",
                paymobOrderId,
                paymentRecord.OrderId);

            paymentRecord.IsProcessed = true;
            paymentRecord.ProcessedAt = DateTime.UtcNow;

            _unitOfWork.PaymentRecords.Update(
                paymentRecord);

            await _unitOfWork.SaveChangesAsync();

            return Ok(
                ApiResponse<object>.Ok(
                    null!,
                    "Payment failure recorded"));
        }
    }
}