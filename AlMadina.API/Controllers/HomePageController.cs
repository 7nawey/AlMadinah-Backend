using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/home-page")]
    [Produces("application/json")]
    public class HomePageController : ControllerBase
    {
        private readonly IHomePageService _homePageService;

        public HomePageController(IHomePageService homePageService)
        {
            _homePageService = homePageService;
        }

        [HttpGet("config")]
        [AllowAnonymous]
        [OutputCache(Duration = 60)]
        public async Task<IActionResult> GetConfig()
        {
            var config = await _homePageService.GetConfigAsync();
            return Ok(ApiResponse<HomePageConfigDto>.Ok(config));
        }

        [HttpPut("category-order")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategoryOrder([FromBody] UpdateCategoryOrderDto dto)
        {
            await _homePageService.UpdateCategoryOrderAsync(dto);
            return Ok(ApiResponse<object>.Ok(null!, "Category order updated"));
        }

        [HttpPut("featured-product")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFeaturedProduct([FromBody] UpdateFeaturedProductDto dto)
        {
            await _homePageService.UpdateFeaturedProductAsync(dto);
            return Ok(ApiResponse<object>.Ok(null!, "Featured product updated"));
        }

        [HttpPut("featured-products/order")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFeaturedOrder([FromBody] UpdateFeaturedProductsOrderDto dto)
        {
            await _homePageService.UpdateFeaturedProductsOrderAsync(dto);
            return Ok(ApiResponse<object>.Ok(null!, "Featured products order updated"));
        }
    }
}
