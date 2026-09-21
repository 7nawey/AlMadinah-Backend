using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/stock-movements")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class StockMovementsController : ControllerBase
    {
        private readonly IStockMovementService _stockMovementService;
        private readonly ILogger<StockMovementsController> _logger;

        public StockMovementsController(IStockMovementService stockMovementService, ILogger<StockMovementsController> logger)
        {
            _stockMovementService = stockMovementService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var movements = await _stockMovementService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<StockMovementDto>>.Ok(movements, "Stock movements retrieved", movements.Count()));
        }

        [HttpGet("product/{productId:guid}")]
        public async Task<IActionResult> GetByProduct(Guid productId)
        {
            var movements = await _stockMovementService.GetByProductAsync(productId);
            return Ok(ApiResponse<IEnumerable<StockMovementDto>>.Ok(movements, "Stock movements retrieved", movements.Count()));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStockMovementDto dto)
        {
            _logger.LogInformation("Admin adding stock movement for product {ProductId}, quantity {Quantity}", dto.ProductId, dto.Quantity);
            var result = await _stockMovementService.AddAsync(dto);
            return Ok(ApiResponse<StockMovementDto>.Ok(result, "Stock movement recorded"));
        }

        [HttpPost("with-new-product")]
        public async Task<IActionResult> CreateWithNewProduct([FromBody] CreateStockMovementWithNewProductDto dto)
        {
            _logger.LogInformation("Admin creating new product with initial stock: {NameAr}", dto.NameAr);
            var result = await _stockMovementService.AddWithNewProductAsync(dto);
            return Ok(ApiResponse<StockMovementDto>.Ok(result, "Product and stock movement created"));
        }
    }
}
