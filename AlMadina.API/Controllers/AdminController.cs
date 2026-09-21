using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminUserService adminUserService, ILogger<AdminController> logger)
        {
            _adminUserService = adminUserService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("Admin {User} listing all users", User.Identity?.Name);
            var users = await _adminUserService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users, "Users retrieved", users.Count()));
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _adminUserService.GetByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<UserDto>.Ok(user));
        }

        [HttpPost("users/{id}/block")]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var result = await _adminUserService.ToggleBlockAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(null!, "User block status toggled"));
        }

        [HttpPost("users/{id}/flag")]
        public async Task<IActionResult> ToggleFlag(string id)
        {
            var result = await _adminUserService.ToggleFlagAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(null!, "User flag status toggled"));
        }

        [HttpGet("users/{id}/orders")]
        public async Task<IActionResult> GetUserOrders(string id)
        {
            var orders = await _adminUserService.GetUserOrdersAsync(id);
            return Ok(ApiResponse<IEnumerable<OrderDto>>.Ok(orders, "User orders retrieved", orders.Count()));
        }
    }
}
