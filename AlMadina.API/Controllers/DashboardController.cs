using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            _logger.LogInformation("Admin {User} requested dashboard stats", User.Identity?.Name);
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(ApiResponse<object>.Ok(stats));
        }

        [HttpGet("best-selling")]
        public async Task<IActionResult> GetBestSelling()
        {
            var items = await _dashboardService.GetBestSellingAsync();
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("most-profitable")]
        public async Task<IActionResult> GetMostProfitable()
        {
            var items = await _dashboardService.GetMostProfitableAsync();
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("recent-orders")]
        public async Task<IActionResult> GetRecentOrders()
        {
            var orders = await _dashboardService.GetRecentOrdersAsync();
            return Ok(ApiResponse<object>.Ok(orders));
        }

        [HttpGet("sales-chart")]
        public async Task<IActionResult> GetSalesChart([FromQuery] int days = 30)
        {
            var data = await _dashboardService.GetSalesChartAsync(days);
            return Ok(ApiResponse<object>.Ok(data));
        }
    }
}
