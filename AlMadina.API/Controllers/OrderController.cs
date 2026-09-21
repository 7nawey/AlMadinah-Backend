using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private bool IsAdmin => User.IsInRole("Admin");

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            _logger.LogInformation("User {UserId} creating order", UserId);
            var result = await _orderService.CreateAsync(dto, UserId);
            return Ok(ApiResponse<OrderResultDto>.Ok(result, "Order created successfully"));
        }

        [HttpPost("in-store")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateInStore([FromBody] CreateInStoreOrderDto dto)
        {
            var result = await _orderService.CreateInStoreAsync(dto, UserId);
            return Ok(ApiResponse<OrderResultDto>.Ok(result, "In-store order created successfully"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PagedRequest? paging = null)
        {
            if (IsAdmin)
            {
                var orders = await _orderService.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<OrderDto>>.Ok(orders, "Orders retrieved", orders.Count()));
            }

            var myOrders = await _orderService.GetByUserIdAsync(UserId);
            return Ok(ApiResponse<IEnumerable<OrderDto>>.Ok(myOrders, "Your orders retrieved", myOrders.Count()));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var orders = await _orderService.GetByUserIdAsync(UserId);
            return Ok(ApiResponse<IEnumerable<OrderDto>>.Ok(orders, "Your orders retrieved", orders.Count()));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
                return NotFound(ApiResponse<object>.Fail("Order not found"));

            if (!IsAdmin && order.UserId != UserId)
            {
                _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to {OwnerId}", UserId, id, order.UserId);
                return Forbid();
            }

            return Ok(ApiResponse<OrderDto>.Ok(order));
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            dto.OrderId = id;
            var result = await _orderService.UpdateStatusAsync(dto);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Order not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Order status updated"));
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var result = await _orderService.CancelAsync(id, UserId, IsAdmin);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Order not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Order cancelled"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _orderService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Order not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Order deleted"));
        }
    }
}
