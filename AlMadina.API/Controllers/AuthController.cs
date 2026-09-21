using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlMadina.Application.Common;
using AlMadina.Application.DTOs;
using AlMadina.Application.Interfaces.Services;

namespace AlMadina.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid request data"));

            var result = await _authService.RegisterAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { token = result }, "Registration successful"));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid request data"));

            var result = await _authService.LoginAsync(dto);
            if (result == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials"));

            return Ok(ApiResponse<object>.Ok(new { token = result }, "Login successful"));
        }

        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            await _authService.SendOtpAsync(dto.Email);
            return Ok(ApiResponse<object>.Ok(null!, "OTP sent successfully"));
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _authService.VerifyOtpAsync(dto);
            if (result == null)
                return BadRequest(ApiResponse<object>.Fail("Invalid OTP"));

            return Ok(ApiResponse<object>.Ok(result, "OTP verified"));
        }

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await _authService.GoogleLoginAsync(dto.IdToken);
            return Ok(ApiResponse<object>.Ok(result, "Google login successful"));
        }

        [HttpGet("profile/{id}")]
        [Authorize]
        public async Task<IActionResult> Profile(string id)
        {
            var user = await _authService.GetProfileAsync(id);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(user));
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
        {
            var result = await _authService.UpdateUserAsync(dto);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(null!, "Profile updated"));
        }
    }
}
