using CvCreator.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CvCreator.API.Controllers;

[Route("api")]
[ApiController]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpDelete("users")]
    public async Task<IActionResult> DeleteUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token.");
        }

        Console.WriteLine(userId);

        var success = await _userService.DeleteUserFromSupabase(userId);

        if (!success)
        {
            return StatusCode(500, "Failed to delete user from Supabase.");
        }

        await _userService.DeleteRelatedData(Guid.Parse(userId));

        return Ok(new { message = "Account successfully deleted." });
    }
}
