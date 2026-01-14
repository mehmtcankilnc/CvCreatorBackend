using Asp.Versioning;
using CvCreator.API.Constants;
using CvCreator.Application.Common.Models;
using CvCreator.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CvCreator.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{apiVersion:apiVersion}/users")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.StandardTraffic)]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpDelete]
    public async Task<IActionResult> DeleteUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var success = await _userService.DeleteUserAccountAsync(userId);

        if (!success)
        {
            return StatusCode(500, new Result { IsSuccess = false, Message = "Kullanıcı silinirken bir hata oluştu." });
        }

        return Ok(new Result { IsSuccess = true, Message = "Kullanıcı ve tüm verileri başarıyla silindi." });
    }
}
