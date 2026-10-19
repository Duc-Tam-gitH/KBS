using System.Security.Claims;
using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IAuthService authService, IUserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await authService.GetProfileAsync(UserId());
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        try { return Ok(await authService.UpdateProfileAsync(UserId(), dto)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        try { await authService.ChangePasswordAsync(UserId(), dto); return NoContent(); }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAll() => Ok(await userService.GetAllAsync());

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, UpdateUserStatusDto dto)
    {
        var user = await userService.UpdateStatusAsync(id, dto.IsActive);
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}/role")]
    public async Task<IActionResult> UpdateRole(long id, UpdateUserRoleDto dto)
    {
        try
        {
            var user = await userService.UpdateRoleAsync(id, dto.RoleId);
            return user is null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
