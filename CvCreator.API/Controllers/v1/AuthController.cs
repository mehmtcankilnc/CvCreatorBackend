using Asp.Versioning;
using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CvCreator.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{apiVersion:apiVersion}/auth")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await authService.LoginWithGoogleAsync(dto);
            return Ok(result);
        }

        [HttpPost("anonymous-login")]
        public async Task<IActionResult> AnonymousLogin()
        {
            var result = await authService.LoginAsGuestAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetUser()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userDto = await authService.GetUserDetailAsync(Guid.Parse(userIdStr));
            return Ok(userDto);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var result = await authService.RefreshTokenAsync(dto.AccessToken, dto.RefreshToken);
            return Ok(result);
        }
    }
}
