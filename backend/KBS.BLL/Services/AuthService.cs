using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class AuthService : IAuthService
{
    private readonly KbsDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    private readonly IJwtService _jwtService;

    public AuthService(
    KbsDbContext context,
    IJwtService jwtService)
{
    _context = context;
    _jwtService = jwtService;
    _passwordHasher = new PasswordHasher<User>();
}

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
{
    var usernameExists = await _context.Users
        .AnyAsync(u => u.Username == dto.Username);

    if (usernameExists)
    {
        throw new InvalidOperationException("Username đã tồn tại.");
    }

    var emailExists = await _context.Users
        .AnyAsync(u => u.Email == dto.Email);

    if (emailExists)
    {
        throw new InvalidOperationException("Email đã được sử dụng.");
    }

    var customerRole = await _context.Roles
        .SingleOrDefaultAsync(r => r.Name == "Customer");

    if (customerRole is null)
    {
        throw new InvalidOperationException(
            "Không tìm thấy role Customer trong database.");
    }

    var user = new User
    {
        Username = dto.Username.Trim(),
        Email = dto.Email.Trim(),
        FullName = dto.FullName?.Trim(),
        PhoneNumber = dto.PhoneNumber?.Trim(),
        RoleId = customerRole.Id,
        IsActive = true
    };

    user.PasswordHash = _passwordHasher.HashPassword(
        user, dto.Password);

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return new AuthResponseDto
{
    AccessToken = _jwtService.GenerateAccessToken(
        user, customerRole.Name),
    User = new UserProfileDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        RoleName = customerRole.Name
    }
};
}


    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
{
    var identifier = dto.LoginIdentifier.Trim();

    var user = await _context.Users
        .Include(u => u.Role)
        .SingleOrDefaultAsync(u =>
            u.Username == identifier || u.Email == identifier);

    if (user is null || !user.IsActive)
    {
        throw new UnauthorizedAccessException(
            "Thông tin đăng nhập không hợp lệ.");
    }

    var verificationResult = _passwordHasher.VerifyHashedPassword(
        user,
        user.PasswordHash,
        dto.Password);

    if (verificationResult == PasswordVerificationResult.Failed)
    {
        throw new UnauthorizedAccessException(
            "Thông tin đăng nhập không hợp lệ.");
    }

    return new AuthResponseDto
{
    AccessToken = _jwtService.GenerateAccessToken(
        user, user.Role?.Name ?? string.Empty),
    User = new UserProfileDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        RoleName = user.Role?.Name ?? string.Empty
    }
};
}
}