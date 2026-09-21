using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;
using AlMadina.Application.Interfaces;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Duration = 120)]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(categories, "Categories retrieved", categories.Count()));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [OutputCache(Duration = 120)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound(ApiResponse<object>.Fail("Category not found"));

            return Ok(ApiResponse<CategoryDto>.Ok(category));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] CreateCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);
            return Ok(ApiResponse<CategoryDto>.Ok(result, "Category created successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateCategoryDto dto)
        {
            dto.Id = id;
            var result = await _categoryService.UpdateAsync(dto);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Category not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Category updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _categoryService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Category not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Category deleted successfully"));
        }
    }
}
