using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Domain.Entities;
using AlMadina.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlMadina.Infrastructure.Services
{
    public class PaymobService : IPaymobService
    {
        private readonly PaymobSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymobService> _logger;

        public PaymobService(
            IOptions<PaymobSettings> options,
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork,
            ILogger<PaymobService> logger)
        {
            _settings = options.Value;
            _unitOfWork = unitOfWork;
            _logger = logger;

            _httpClient =
                httpClientFactory.CreateClient();

            _httpClient.BaseAddress =
                new Uri(_settings.BaseUrl);

            _httpClient.DefaultRequestHeaders.Accept.Clear();

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                    "application/json"));
        }

        // ============================================================
        // INITIATE PAYMENT
        // ============================================================

        public async Task<PaymobInitiatePaymentResponseDto>
            InitiatePaymentAsync(
                PaymobInitiatePaymentDto dto,
                string customerEmail,
                string customerFirstName)
        {
            // --------------------------------------------------------
            // 1. Load internal order
            // --------------------------------------------------------

            var order =
                await _unitOfWork.Orders
                    .GetAllQueryable()
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(
                        o => o.Id == dto.OrderId);

            if (order == null)
            {
                throw new Exception(
                    $"Order {dto.OrderId} not found.");
            }

            // --------------------------------------------------------
            // 2. Validate order status
            // --------------------------------------------------------

            if (order.Status != OrderStatus.PendingPayment)
            {
                throw new Exception(
                    "Order is not in pending payment state.");
            }

            // --------------------------------------------------------
            // 3. Validate payment state
            // --------------------------------------------------------

            if (order.IsPaid)
            {
                throw new Exception(
                    "Order is already paid.");
            }

            // --------------------------------------------------------
            // 3.5 Resolve payment method -> integration ID
            // --------------------------------------------------------

            var paymentMethod =
                (dto.PaymentMethod ?? "card")
                    .Trim()
                    .ToLowerInvariant();

            int integrationId;

            string? walletPhone = null;

            switch (paymentMethod)
            {
                case "card":
                {
                    integrationId =
                        _settings.CardIntegrationId;

                    break;
                }

                case "wallet":
                {
                    integrationId =
                        _settings.WalletIntegrationId;

                    walletPhone =
                        NormalizeWalletPhone(
                            dto.WalletPhone);

                    break;
                }

                default:
                {
                    throw new Exception(
                        "Unsupported payment method. " +
                        "Use 'card' or 'wallet'.");
                }
            }

            // --------------------------------------------------------
            // 3.6 Validate currency — never trust the client here.
            // Only EGP is supported; empty defaults to EGP.
            // --------------------------------------------------------

            var currency =
                string.IsNullOrWhiteSpace(dto.Currency)
                    ? "EGP"
                    : dto.Currency.Trim().ToUpperInvariant();

            if (currency != "EGP")
            {
                throw new Exception(
                    "Unsupported currency. Only 'EGP' is accepted.");
            }

            // --------------------------------------------------------
            // 4. Calculate amount
            // --------------------------------------------------------

            var amountCents =
                (int)Math.Round(
                    order.TotalPrice * 100,
                    MidpointRounding.AwayFromZero);

            if (amountCents <= 0)
            {
                throw new Exception(
                    "Order amount must be greater than zero.");
            }

            _logger.LogInformation(
                "Creating Paymob payment for internal order {OrderId}, amount={AmountCents} cents, method={PaymentMethod}",
                order.Id,
                amountCents,
                paymentMethod);

            // ========================================================
            // STEP 1: AUTH TOKEN
            // ========================================================

            var tokenResponse =
                await GetAuthTokenAsync();

            // ========================================================
            // STEP 2: CREATE PAYMOB ORDER
            // ========================================================

            var orderItems =
                order.Items
                    .Select(i => new PaymobOrderItemDto
                    {
                        Name =
                            i.Product?.NameAr ??
                            "Product",

                        AmountCents =
                            (int)Math.Round(
                                i.UnitPrice * 100,
                                MidpointRounding.AwayFromZero),

                        Description =
                            i.Product?.NameAr ??
                            string.Empty,

                        Quantity =
                            i.Quantity
                    })
                    .ToList();

            var orderRequest =
                new PaymobOrderRequestDto
                {
                    AuthToken =
                        tokenResponse.Token,

                    AmountCents =
                        amountCents,

                    Currency =
                        currency,

                    IntegrationId =
                        integrationId,

                    Items =
                        orderItems
                };

            var orderResponse =
                await CreateOrderAsync(
                    orderRequest);

            _logger.LogInformation(
                "Paymob order created successfully. InternalOrderId={InternalOrderId}, PaymobOrderId={PaymobOrderId}",
                order.Id,
                orderResponse.Id);

            // ========================================================
            // STEP 2.5: SAVE PAYMENT RECORD
            // ========================================================

            var paymentRecord =
                new PaymentRecord
                {
                    Id = Guid.NewGuid(),

                    OrderId =
                        order.Id,

                    PaymobOrderId =
                        orderResponse.Id,

                    IntegrationId =
                        integrationId,

                    AmountCents =
                        amountCents,

                    Currency =
                        currency,

                    IsProcessed =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _unitOfWork.PaymentRecords
                .AddAsync(paymentRecord);

            await _unitOfWork.SaveChangesAsync();

            // ========================================================
            // STEP 3: GENERATE PAYMENT KEY
            // ========================================================

            var paymentKeyRequest =
                new PaymobPaymentKeyRequestDto
                {
                    AuthToken =
                        tokenResponse.Token,

                    AmountCents =
                        amountCents,

                    OrderId =
                        orderResponse.Id,

                    Currency =
                        currency,

                    IntegrationId =
                        integrationId,

                    BillingData =
                        new PaymobBillingDataDto
                        {
                            Email =
                                customerEmail,

                            FirstName =
                                customerFirstName,

                            PhoneNumber =
                                order.CustomerPhone,

                            Apartment =
                                "NA",

                            Floor =
                                "NA",

                            Street =
                                "NA",

                            Building =
                                "NA",

                            ShippingMethod =
                                "NA",

                            PostalCode =
                                "NA",

                            City =
                                "NA",

                            Country =
                                "EG",

                            LastName =
                                "Customer",

                            State =
                                "NA"
                        },

                    // Redirect user after payment
                    RedirectionUrl =
                        _settings.ReturnUrl
                };

            var paymentKeyResponse =
                await GeneratePaymentKeyAsync(
                    paymentKeyRequest);

            // ========================================================
            // STEP 4: BUILD CHECKOUT TARGET
            // ========================================================

            string iframeUrl = string.Empty;
            string redirectUrl = string.Empty;

            if (paymentMethod == "wallet")
            {
                // Wallet (UIG / online_new):
                // initiate the payment server-side and redirect the
                // customer to the wallet provider confirmation page.
                redirectUrl =
                    await PayWithWalletAsync(
                        paymentKeyResponse.Token,
                        walletPhone!);

                _logger.LogInformation(
                    "Paymob wallet payment initialized for InternalOrderId={InternalOrderId}, PaymobOrderId={PaymobOrderId}",
                    order.Id,
                    orderResponse.Id);
            }
            else
            {
                // Card: existing hosted iframe behavior.
                iframeUrl =
                    $"{_settings.IframeBaseUrl}" +
                    $"/acceptance/iframes/{_settings.IframeId}" +
                    $"?payment_token={paymentKeyResponse.Token}";

                _logger.LogInformation(
                    "Paymob card payment initialized successfully for InternalOrderId={InternalOrderId}, PaymobOrderId={PaymobOrderId}",
                    order.Id,
                    orderResponse.Id);
            }

            return new PaymobInitiatePaymentResponseDto
            {
                PaymentToken =
                    paymentKeyResponse.Token,

                OrderId =
                    orderResponse.Id,

                IframeUrl =
                    iframeUrl,

                RedirectUrl =
                    redirectUrl
            };
        }

        // ============================================================
        // VERIFY PAYMOB CALLBACK HMAC
        // ============================================================

        public async Task<bool> VerifyCallbackAsync(
            PaymobCallbackDto callback,
            string receivedHmac)
        {
            if (callback.Obj.ValueKind !=
                JsonValueKind.Object)
            {
                _logger.LogWarning(
                    "Paymob callback obj is not a JSON object");

                return false;
            }

            // --------------------------------------------------------
            // HMAC must exist
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(receivedHmac))
            {
                _logger.LogWarning(
                    "Paymob callback HMAC is missing");

                return false;
            }

            // --------------------------------------------------------
            // HMAC secret must exist
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    _settings.HmacSecret))
            {
                _logger.LogError(
                    "Paymob HMAC secret is not configured");

                return false;
            }

            // --------------------------------------------------------
            // Log Paymob order
            // --------------------------------------------------------

            if (callback.Obj.TryGetProperty(
                    "order",
                    out var orderProp) &&
                orderProp.ValueKind ==
                    JsonValueKind.Object &&
                orderProp.TryGetProperty(
                    "id",
                    out var orderIdProp))
            {
                _logger.LogInformation(
                    "Verifying HMAC for Paymob order {OrderId}",
                    orderIdProp.GetRawText());
            }

            // --------------------------------------------------------
            // Build exact HMAC string
            // --------------------------------------------------------

            var hmacString =
                BuildHmacString(callback);

            if (string.IsNullOrEmpty(hmacString))
            {
                _logger.LogWarning(
                    "Could not build Paymob HMAC string");

                return false;
            }

            // --------------------------------------------------------
            // Calculate HMAC SHA512
            // --------------------------------------------------------

            var computedHmac =
                ComputeHmacSha512(
                    hmacString,
                    _settings.HmacSecret);

            // --------------------------------------------------------
            // Compare signatures
            // --------------------------------------------------------

            var isValid =
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(
                        computedHmac.ToLowerInvariant()),
                    Encoding.UTF8.GetBytes(
                        receivedHmac.Trim()
                            .ToLowerInvariant()));

            if (!isValid)
            {
                _logger.LogWarning(
                    "Paymob HMAC verification failed");

                // DO NOT log HMAC secret.
                // DO NOT log credentials.

                _logger.LogDebug(
                    "Computed HMAC: {ComputedHmac}",
                    computedHmac);

                _logger.LogDebug(
                    "Received HMAC: {ReceivedHmac}",
                    receivedHmac);
            }
            else
            {
                _logger.LogInformation(
                    "Paymob HMAC verification succeeded");
            }

            return isValid;
        }

        // ============================================================
        // GET AUTH TOKEN
        // ============================================================

        private async Task<PaymobTokenResponseDto>
            GetAuthTokenAsync()
        {
            var request = new
            {
                api_key =
                    _settings.ApiKey
            };

            var json =
                JsonSerializer.Serialize(request);

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await _httpClient.PostAsync(
                    "auth/tokens",
                    content);

            var responseContent =
                await response.Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Paymob Auth Error: " +
                    $"{response.StatusCode} - " +
                    $"{responseContent}");
            }

            var result =
                JsonSerializer.Deserialize<
                    PaymobTokenResponseDto>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });

            if (result == null ||
                string.IsNullOrWhiteSpace(
                    result.Token))
            {
                throw new Exception(
                    "Paymob did not return authentication token.");
            }

            return result;
        }

        // ============================================================
        // CREATE PAYMOB ORDER
        // ============================================================

        private async Task<PaymobOrderResponseDto>
            CreateOrderAsync(
                PaymobOrderRequestDto dto)
        {
            var json =
                JsonSerializer.Serialize(
                    dto,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.SnakeCaseLower
                    });

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await _httpClient.PostAsync(
                    "ecommerce/orders",
                    content);

            var responseContent =
                await response.Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Paymob Order Error: " +
                    $"{response.StatusCode} - " +
                    $"{responseContent}");
            }

            using var doc =
                JsonDocument.Parse(
                    responseContent);

            if (!doc.RootElement.TryGetProperty(
                    "id",
                    out var idProperty))
            {
                throw new Exception(
                    "Paymob order response does not contain an id.");
            }

            return new PaymobOrderResponseDto
            {
                Id =
                    idProperty.GetInt32()
            };
        }

        // ============================================================
        // GENERATE PAYMENT KEY
        // ============================================================

        private async Task<
            PaymobPaymentKeyResponseDto>
            GeneratePaymentKeyAsync(
                PaymobPaymentKeyRequestDto dto)
        {
            var json =
                JsonSerializer.Serialize(
                    dto,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.SnakeCaseLower
                    });

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await _httpClient.PostAsync(
                    "acceptance/payment_keys",
                    content);

            var responseContent =
                await response.Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Paymob Payment Key Error: " +
                    $"{response.StatusCode} - " +
                    $"{responseContent}");
            }

            using var doc =
                JsonDocument.Parse(
                    responseContent);

            if (!doc.RootElement.TryGetProperty(
                    "token",
                    out var tokenProperty))
            {
                throw new Exception(
                    "Paymob payment key response does not contain token.");
            }

            var token =
                tokenProperty.GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception(
                    "Paymob returned an empty payment token.");
            }

            return new PaymobPaymentKeyResponseDto
            {
                Token =
                    token
            };
        }

        // ============================================================
        // PAY WITH MOBILE WALLET
        // POST /acceptance/payments/pay
        // ============================================================

        private async Task<string> PayWithWalletAsync(
            string paymentToken,
            string walletPhone)
        {
            var request =
                new PaymobWalletPayRequestDto
                {
                    PaymentToken =
                        paymentToken,

                    Source =
                        new PaymobWalletSourceDto
                        {
                            Identifier =
                                walletPhone,

                            Subtype =
                                "WALLET"
                        }
                };

            var json =
                JsonSerializer.Serialize(request);

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await _httpClient.PostAsync(
                    "acceptance/payments/pay",
                    content);

            var responseContent =
                await response.Content
                    .ReadAsStringAsync();

            _logger.LogInformation(
                "Paymob wallet response. StatusCode={StatusCode}, Body={Body}",
                response.StatusCode,
                responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Paymob Wallet Error: " +
                    $"{response.StatusCode} - " +
                    $"{responseContent}");
            }

            using var doc =
                JsonDocument.Parse(
                    responseContent);

            var root =
                doc.RootElement;

            // ----------------------------------------------------
            // pending / success flags
            // ----------------------------------------------------

            var pending =
                root.TryGetProperty(
                    "pending",
                    out var pendingProp) &&
                pendingProp.ValueKind ==
                    JsonValueKind.True;

            var success =
                root.TryGetProperty(
                    "success",
                    out var successProp) &&
                successProp.ValueKind ==
                    JsonValueKind.True;

            // ----------------------------------------------------
            // data object (may not exist)
            // ----------------------------------------------------

            var hasData =
                root.TryGetProperty(
                    "data",
                    out var dataProp) &&
                dataProp.ValueKind ==
                    JsonValueKind.Object;

            // ----------------------------------------------------
            // Extract redirect URL in order:
            //   data.redirect_url -> redirect_url -> iframe_redirection_url
            // ----------------------------------------------------

            string? redirectUrl = null;

            if (hasData &&
                dataProp.TryGetProperty(
                    "redirect_url",
                    out var dataRedirectProp) &&
                dataRedirectProp.ValueKind ==
                    JsonValueKind.String)
            {
                redirectUrl =
                    dataRedirectProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(
                    redirectUrl) &&
                root.TryGetProperty(
                    "redirect_url",
                    out var redirectProp) &&
                redirectProp.ValueKind ==
                    JsonValueKind.String)
            {
                redirectUrl =
                    redirectProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(
                    redirectUrl) &&
                root.TryGetProperty(
                    "iframe_redirection_url",
                    out var iframeProp) &&
                iframeProp.ValueKind ==
                    JsonValueKind.String)
            {
                redirectUrl =
                    iframeProp.GetString();
            }

            // ----------------------------------------------------
            // pending=true: initiation accepted, user must complete
            // the payment through the wallet provider redirect.
            // NOT a proof of payment - payment is finalized only
            // via the Paymob callback after HMAC verification.
            // ----------------------------------------------------

            if (pending)
            {
                if (!string.IsNullOrWhiteSpace(
                        redirectUrl))
                {
                    return redirectUrl;
                }

                throw new Exception(
                    "Paymob wallet payment is pending but no redirect URL was returned.");
            }

            // ----------------------------------------------------
            // pending=false + success=true
            // ----------------------------------------------------

            if (success)
            {
                if (!string.IsNullOrWhiteSpace(
                        redirectUrl))
                {
                    return redirectUrl;
                }

                throw new Exception(
                    "Paymob wallet payment succeeded but no redirect URL was returned.");
            }

            // ----------------------------------------------------
            // pending=false + success=false: failed.
            // Read error message from data.message when present.
            // ----------------------------------------------------

            string? errorMessage = null;

            if (hasData &&
                dataProp.TryGetProperty(
                    "message",
                    out var messageProp) &&
                messageProp.ValueKind ==
                    JsonValueKind.String)
            {
                errorMessage =
                    messageProp.GetString();
            }

            throw new Exception(
                string.IsNullOrWhiteSpace(
                    errorMessage)
                    ? "Wallet payment failed."
                    : $"Wallet payment failed: {errorMessage}");
        }

        // ============================================================
        // NORMALIZE WALLET PHONE NUMBER
        // Accepts 01xxxxxxxxx / 201xxxxxxxxx / +201xxxxxxxxx
        // and returns +20xxxxxxxxx
        // ============================================================

        private static string NormalizeWalletPhone(
            string? walletPhone)
        {
            if (string.IsNullOrWhiteSpace(
                    walletPhone))
            {
                throw new Exception(
                    "Wallet phone number is required for wallet payments.");
            }

            var digits =
                new string(
                    walletPhone
                        .Where(char.IsDigit)
                        .ToArray());

            if (digits.StartsWith("0020"))
            {
                digits = digits[4..];
            }
            else if (digits.StartsWith("20") &&
                     digits.Length == 12)
            {
                digits = digits[2..];
            }
            else if (digits.StartsWith("2") &&
                     digits.Length == 11)
            {
                digits = digits[1..];
            }

            if (digits.Length != 11 ||
                !digits.StartsWith("01"))
            {
                throw new Exception(
                    "Invalid wallet phone number. " +
                    "Use the format 01xxxxxxxxx.");
            }

            return $"+2{digits}";
        }

        // ============================================================
        // BUILD PAYMOB HMAC STRING
        // ============================================================

        private static string BuildHmacString(
            PaymobCallbackDto callback)
        {
            if (callback.Obj.ValueKind !=
                JsonValueKind.Object)
            {
                return string.Empty;
            }

            var obj =
                callback.Obj;

            // --------------------------------------------------------
            // Get primitive property
            // --------------------------------------------------------

            string GetValue(
                string propertyName)
            {
                if (!obj.TryGetProperty(
                        propertyName,
                        out var prop))
                {
                    return string.Empty;
                }

                return prop.ValueKind switch
                {
                    JsonValueKind.String =>
                        prop.GetString() ??
                        string.Empty,

                    JsonValueKind.Number =>
                        prop.GetRawText(),

                    JsonValueKind.True =>
                        "true",

                    JsonValueKind.False =>
                        "false",

                    JsonValueKind.Null =>
                        string.Empty,

                    _ =>
                        prop.GetRawText()
                };
            }

            // --------------------------------------------------------
            // Get order.id
            // --------------------------------------------------------

            string GetOrderId()
            {
                if (!obj.TryGetProperty(
                        "order",
                        out var orderProp))
                {
                    return string.Empty;
                }

                if (orderProp.ValueKind !=
                    JsonValueKind.Object)
                {
                    return string.Empty;
                }

                if (!orderProp.TryGetProperty(
                        "id",
                        out var idProp))
                {
                    return string.Empty;
                }

                return idProp.ValueKind switch
                {
                    JsonValueKind.Number =>
                        idProp.GetRawText(),

                    JsonValueKind.String =>
                        idProp.GetString() ??
                        string.Empty,

                    _ =>
                        string.Empty
                };
            }

            // --------------------------------------------------------
            // Get source_data property
            // --------------------------------------------------------

            string GetSourceData(
                string propertyName)
            {
                if (!obj.TryGetProperty(
                        "source_data",
                        out var sourceData))
                {
                    return string.Empty;
                }

                if (sourceData.ValueKind !=
                    JsonValueKind.Object)
                {
                    return string.Empty;
                }

                if (!sourceData.TryGetProperty(
                        propertyName,
                        out var value))
                {
                    return string.Empty;
                }

                return value.ValueKind switch
                {
                    JsonValueKind.String =>
                        value.GetString() ??
                        string.Empty,

                    JsonValueKind.Number =>
                        value.GetRawText(),

                    JsonValueKind.True =>
                        "true",

                    JsonValueKind.False =>
                        "false",

                    JsonValueKind.Null =>
                        string.Empty,

                    _ =>
                        value.GetRawText()
                };
            }

            // ========================================================
            // IMPORTANT:
            // EXACT PAYMOB HMAC FIELD ORDER
            // ========================================================
            var parts = new[]
            {
    GetValue("amount_cents"), // مهم جدًا
    GetValue("created_at"),
    GetValue("currency"),
    GetValue("error_occured"),
    GetValue("has_parent_transaction"),
    GetValue("id"),
    GetValue("integration_id"),
    GetValue("is_3d_secure"),
    GetValue("is_auth"),
    GetValue("is_capture"),
    GetValue("is_refunded"),
    GetValue("is_standalone_payment"),
    GetValue("is_voided"),
    GetOrderId(),
    GetValue("owner"),
    GetValue("pending"),
    GetSourceData("pan"),
    GetSourceData("sub_type"),
    GetSourceData("type"),
    GetValue("success")
};

            return string.Concat(parts);
        }

        // ============================================================
        // HMAC SHA512
        // ============================================================

        private static string ComputeHmacSha512(
            string input,
            string key)
        {
            using var hmac =
                new HMACSHA512(
                    Encoding.UTF8.GetBytes(key));

            var hash =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(input));

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }
    }
}