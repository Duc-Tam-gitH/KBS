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
[TypeFilter(typeof(StaffOperationFilter))]
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
        return Ok(await authService.UpdateProfileAsync(UserId(), dto));
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        try
        {
            await authService.ChangePasswordAsync(UserId(), dto);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAll() => Ok(await userService.GetAllAsync());

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, UpdateUserStatusDto dto)
    {
        var user = await userService.UpdateStatusAsync(id, dto.IsActive!.Value, UserId());
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}/role")]
    public async Task<IActionResult> UpdateRole(long id, UpdateUserRoleDto dto)
    {
        try
        {
            var user = await userService.UpdateRoleAsync(id, dto.RoleId, UserId());
            return user is null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize(Roles = "Admin")]
    [HttpGet("roles")]
    public Task<List<UserRoleDto>> Roles() => userService.RolesAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> Create(CreateUserDto dto) =>
        StatusCode(201, await userService.CreateAsync(dto));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public Task<UserResponseDto> Save(long id, SaveUserDto dto) => userService.SaveAsync(id, dto, UserId());

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await userService.DeleteAsync(id, UserId());
        return NoContent();
    }
}
