using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/returns")]
    [Produces("application/json")]
    [Authorize]
    public class ReturnsController : ControllerBase
    {
        private readonly IReturnService _returnService;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(IReturnService returnService, ILogger<ReturnsController> logger)
        {
            _returnService = returnService;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private bool IsAdmin => User.IsInRole("Admin");

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateReturnRequestDto dto)
        {
            _logger.LogInformation("Admin {UserId} creating return for order {OrderId}", UserId, dto.OrderId);
            var result = await _returnService.CreateAsync(dto, UserId);
            return Ok(ApiResponse<ReturnRequestDto>.Ok(result, "Return processed successfully"));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyReturns()
        {
            var returns = await _returnService.GetByUserAsync(UserId);
            return Ok(ApiResponse<IEnumerable<ReturnRequestDto>>.Ok(returns, "Your return requests retrieved", returns.Count()));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var returns = await _returnService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<ReturnRequestDto>>.Ok(returns, "All return requests retrieved", returns.Count()));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var returnRequest = await _returnService.GetByIdAsync(id);
            if (returnRequest == null)
                return NotFound(ApiResponse<object>.Fail("Return request not found"));

            if (!IsAdmin && returnRequest.UserId != UserId)
            {
                _logger.LogWarning("User {UserId} attempted to access return {ReturnId} belonging to {OwnerId}", UserId, id, returnRequest.UserId);
                return Forbid();
            }

            return Ok(ApiResponse<ReturnRequestDto>.Ok(returnRequest));
        }

        [HttpPut("{id:guid}/process")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Process(Guid id, [FromBody] ProcessReturnDto dto)
        {
            dto.ReturnRequestId = id;
            var result = await _returnService.ProcessAsync(dto);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Return request not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Return request processed"));
        }
    }
}
