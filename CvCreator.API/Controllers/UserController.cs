using CvCreator.API.Constants;
using CvCreator.Application.Common.Models;
using CvCreator.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CvCreator.API.Controllers;

[Route("api")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.StandardTraffic)]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpDelete("users")]
    public async Task<IActionResult> DeleteUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var success = await _userService.DeleteUserFromSupabase(userId);

        if (!success)
        {
            return StatusCode(500, new Result { IsSuccess = false, Message = "Kullanıcı Supabase'den silinemedi." });
        }

        await _userService.DeleteRelatedData(Guid.Parse(userId));

        return Ok(new Result { IsSuccess = true, Message = "Kullanıcı silindi."});
    }
}
