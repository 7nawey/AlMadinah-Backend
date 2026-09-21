using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/deals")]
    [Produces("application/json")]
    public class DealsController : ControllerBase
    {
        private readonly IDealService _dealService;

        public DealsController(IDealService dealService)
        {
            _dealService = dealService;
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Duration = 60)]
        public async Task<IActionResult> GetAll()
        {
            var deals = await _dealService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<DealDto>>.Ok(deals, "Deals retrieved", deals.Count()));
        }

        [HttpGet("active")]
        [AllowAnonymous]
        [OutputCache(Duration = 60)]
        public async Task<IActionResult> GetActive()
        {
            var deals = await _dealService.GetActiveAsync();
            return Ok(ApiResponse<IEnumerable<DealDto>>.Ok(deals, "Active deals retrieved", deals.Count()));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [OutputCache(Duration = 60)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var deal = await _dealService.GetByIdAsync(id);
            if (deal == null)
                return NotFound(ApiResponse<object>.Fail("Deal not found"));

            return Ok(ApiResponse<DealDto>.Ok(deal));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateDealDto dto)
        {
            var result = await _dealService.CreateAsync(dto);
            return Ok(ApiResponse<DealDto>.Ok(result, "Deal created successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealDto dto)
        {
            dto.Id = id;
            var result = await _dealService.UpdateAsync(dto);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Deal not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Deal updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _dealService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Deal not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Deal deleted successfully"));
        }

        [HttpPost("{id:guid}/toggle-active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var result = await _dealService.ToggleActiveAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Deal not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Deal status toggled"));
        }
    }
}
