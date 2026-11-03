using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuickCartApi.Common;
using QuickCartApi.DTOs.Users;
using QuickCartApi.Extensions;
using QuickCartApi.Services.Interfaces;
using System.Security.Claims;

namespace QuickCartApi.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    // PUT api/v1/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await userService.UpdateProfileAsync(userId, request);
        return result.Success ? Ok(result.Value) : result.ToProblem();
    }

    // POST api/v1/users/me/password
    [HttpPost("me/password")]
    [EnableRateLimiting(RateLimitingExtension.AuthPolicy)] // limits guessing of the current password
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await userService.ChangePasswordAsync(userId, request);
        return result.Success ? NoContent() : result.ToProblem();
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}