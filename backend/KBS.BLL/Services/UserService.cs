using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Users;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace KBS.BLL.Services;

public class UserService(KbsDbContext context) : IUserService
{
    public async Task<IReadOnlyList<UserResponseDto>> GetAllAsync()
    {
        var users = await context.Users.AsNoTracking().Include(u => u.Role).OrderBy(u => u.Id).ToListAsync();
        return users.Select(ToDto).ToList();
    }

    public Task<List<UserRoleDto>> RolesAsync() => context.Roles.AsNoTracking().OrderBy(x => x.Id)
        .Select(x => new UserRoleDto(x.Id, x.Name)).ToListAsync();

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        if (dto.Password.Length < 8 || !dto.Password.Any(char.IsLetter) || !dto.Password.Any(char.IsDigit)
            || dto.Password != dto.ConfirmPassword)
            throw new ArgumentException("Mật khẩu cần ít nhất 8 ký tự, gồm chữ và số; xác nhận phải khớp.");
        var user = new User();
        await ApplyAsync(user, dto);
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, dto.Password);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<UserResponseDto> SaveAsync(
        long id,
        SaveUserDto dto,
        long actorId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await FindAsync(id);
        await ProtectAsync(user, actorId, dto.IsActive, dto.RoleId);
        await ApplyAsync(user, dto);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return ToDto(user);
    }

    public async Task<UserResponseDto> UpdateStatusAsync(
        long id,
        bool isActive,
        long actorId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await FindAsync(id);
        await ProtectAsync(user, actorId, isActive, user.RoleId);
        user.IsActive = isActive;
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return ToDto(user);
    }

    public async Task<UserResponseDto> UpdateRoleAsync(
        long id,
        long roleId,
        long actorId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await FindAsync(id);
        await ProtectAsync(user, actorId, user.IsActive, roleId);
        user.Role = await context.Roles.SingleOrDefaultAsync(x => x.Id == roleId)
            ?? throw new ArgumentException("Vai trò không tồn tại.");
        user.RoleId = roleId;
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return ToDto(user);
    }

    private async Task<User> FindAsync(long id) =>
        await context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

    private async Task ProtectAsync(
        User user,
        long actorId,
        bool active,
        long roleId)
    {
        if (user.Id == actorId && (!active || roleId != user.RoleId))
            throw new InvalidOperationException("Không thể tự khóa hoặc thay đổi vai trò của chính mình.");
        if (user.IsActive && user.Role?.Name == "Admin" && (!active || roleId != user.RoleId)
            && !await context.Users.AnyAsync(x => x.Id != user.Id && x.IsActive && x.Role!.Name == "Admin"))
            throw new InvalidOperationException("Phải giữ ít nhất một Admin đang hoạt động.");
    }

    private async Task ApplyAsync(User user, SaveUserDto dto)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        if (username.Length == 0 || email.Length == 0) throw new ArgumentException("Thiếu tên đăng nhập hoặc email.");
        if (await context.Users.AnyAsync(x => x.Id != user.Id
            && (x.Username.ToLower() == username.ToLower() || x.Email.ToLower() == email
                || (phone != null && x.PhoneNumber == phone))))
            throw new InvalidOperationException("Tên đăng nhập, email hoặc số điện thoại đã được sử dụng.");
        var role = await context.Roles.SingleOrDefaultAsync(x => x.Id == dto.RoleId)
            ?? throw new ArgumentException("Vai trò không tồn tại.");
        user.Username = username;
        user.Email = email;
        user.FullName = string.IsNullOrWhiteSpace(dto.FullName) ? null : dto.FullName.Trim();
        user.PhoneNumber = phone;
        user.RoleId = role.Id;
        user.Role = role;
        user.IsActive = dto.IsActive;
    }

    public async Task DeleteAsync(long id, long actorId)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await FindAsync(id);
        if (id == actorId) throw new InvalidOperationException("Không thể tự xóa tài khoản đang đăng nhập.");
        await ProtectAsync(user, actorId, false, user.RoleId);
        if (await context.Orders.AnyAsync(x => x.UserId == id)
            || await context.CartItems.AnyAsync(x => x.UserId == id)
            || await context.Likelist.AnyAsync(x => x.UserId == id)
            || await context.ProductReviews.AnyAsync(x => x.UserId == id)
            || await context.StaffShifts.AnyAsync(x => x.StaffId == id)
            || await context.OrderNotes.AnyAsync(x => x.StaffId == id)
            || await context.InventoryMovements.AnyAsync(x => x.ActorId == id)
            || await context.ChatConversations.AnyAsync(x => x.CustomerId == id || x.StaffId == id)
            || await context.ChatMessages.AnyAsync(x => x.SenderId == id)
            || await context.AiEvents.AnyAsync(x => x.ActorId == id))
            throw new InvalidOperationException("Tài khoản có dữ liệu liên quan. Hãy khóa thay vì xóa.");
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static UserResponseDto ToDto(User user) => new()
    {
        Id = user.Id, Username = user.Username, Email = user.Email, FullName = user.FullName,
        PhoneNumber = user.PhoneNumber, RoleId = user.RoleId, RoleName = user.Role?.Name ?? string.Empty,
        IsActive = user.IsActive, CreatedAt = user.CreatedAt
    };
}
