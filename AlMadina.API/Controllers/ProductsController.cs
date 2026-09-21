using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;
        private readonly IFileService _fileService;

        public ProductsController(
            IProductService productService,
            IFileService fileService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _fileService = fileService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetAll([FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 50;
            var result = await _productService.GetAllAsync(page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Products retrieved", total, page, size));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [OutputCache(Duration = 60)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(ApiResponse<object>.Fail("Product not found"));

            return Ok(ApiResponse<ProductDto>.Ok(product));
        }

        [HttpGet("featured")]
        [AllowAnonymous]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetFeatured([FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 20;
            var result = await _productService.GetFeaturedAsync(page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Featured products retrieved", total, page, size));
        }

        [HttpGet("discounted")]
        [AllowAnonymous]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetDiscounted([FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 20;
            var result = await _productService.GetDiscountedAsync(page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Discounted products retrieved", total, page, size));
        }

        [HttpGet("best-selling")]
        [AllowAnonymous]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetBestSelling([FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 20;
            var result = await _productService.GetBestSellingAsync(page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Best selling products retrieved", total, page, size));
        }

        [HttpGet("category/{categoryId:guid}")]
        [AllowAnonymous]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetByCategory(Guid categoryId, [FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 40;
            var result = await _productService.GetProductsByCategoryAsync(categoryId, page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Products retrieved", total, page, size));
        }

        [HttpGet("search")]
        [AllowAnonymous]
        [OutputCache(Duration = 30, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 20;
            var result = await _productService.SearchAsync(keyword, page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Search results", total, page, size));
        }

        [HttpGet("regular")]
        [AllowAnonymous]
        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "*" })]
        public async Task<IActionResult> GetHomeRegular([FromQuery] PagedRequest? paging = null)
        {
            var page = paging?.PageNumber ?? 1;
            var size = paging?.PageSize ?? 20;
            var result = await _productService.GetHomeRegularProductsAsync(page, size);
            var total = result?.TotalCount ?? 0;
            return Ok(ApiResponse<object>.Ok(result, "Regular products retrieved", total, page, size));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            _logger.LogInformation("Admin creating product: {NameAr}", dto.NameAr);
            var result = await _productService.CreateAsync(dto);
            return Ok(ApiResponse<ProductDto>.Ok(result, "Product created successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            dto.Id = id;
            var result = await _productService.UpdateAsync(dto);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Product not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Product updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Product not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Product deleted successfully"));
        }

        [HttpPost("{id:guid}/upload-image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            var result = await _productService.UploadImageAsync(id, file);
            return Ok(ApiResponse<object>.Ok(new { imageUrl = result }, "Image uploaded successfully"));
        }
    }
}
