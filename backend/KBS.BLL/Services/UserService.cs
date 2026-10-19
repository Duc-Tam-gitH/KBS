using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Users;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class UserService(KbsDbContext context) : IUserService
{
    public async Task<IReadOnlyList<UserResponseDto>> GetAllAsync()
    {
        var users = await context.Users.AsNoTracking().Include(u => u.Role).OrderBy(u => u.Id).ToListAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<UserResponseDto?> UpdateStatusAsync(long id, bool isActive)
    {
        var user = await context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return null;
        user.IsActive = isActive;
        await context.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<UserResponseDto?> UpdateRoleAsync(long id, long roleId)
    {
        var user = await context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == id);
        if (user is null) return null;
        var role = await context.Roles.SingleOrDefaultAsync(r => r.Id == roleId)
            ?? throw new InvalidOperationException("Role không tồn tại.");
        user.RoleId = roleId;
        user.Role = role;
        await context.SaveChangesAsync();
        return ToDto(user);
    }

    private static UserResponseDto ToDto(User user) => new()
    {
        Id = user.Id, Username = user.Username, Email = user.Email, FullName = user.FullName,
        PhoneNumber = user.PhoneNumber, RoleId = user.RoleId, RoleName = user.Role?.Name ?? string.Empty,
        IsActive = user.IsActive, CreatedAt = user.CreatedAt
    };
}
