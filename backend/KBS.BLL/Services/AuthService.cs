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
    var registerDto = dto ?? throw new ArgumentNullException(nameof(dto));
    var username = GetRequiredValue(registerDto.Username, "Username");
    var email = NormalizeEmail(registerDto.Email);
    var password = GetRequiredValue(registerDto.Password, "Password");

    ValidatePassword(password);
    if (password != registerDto.ConfirmPassword)
    {
        throw new InvalidOperationException("Xác nhận mật khẩu không khớp.");
    }

    var usernameExists = await _context.Users
        .AnyAsync(u => u.Username == username);

    if (usernameExists)
    {
        throw new InvalidOperationException("Username đã tồn tại.");
    }

    var emailExists = await _context.Users
        .AnyAsync(u => u.Email == email);

    if (emailExists)
    {
        throw new InvalidOperationException("Email đã được sử dụng.");
    }

    var phoneNumber = TrimToNull(registerDto.PhoneNumber);
    if (phoneNumber is not null && await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber))
    {
        throw new InvalidOperationException("Số điện thoại đã được sử dụng.");
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
        Username = username,
        Email = email,
        FullName = TrimToNull(registerDto.FullName),
        PhoneNumber = phoneNumber,
        RoleId = customerRole.Id,
        IsActive = true
    };

    user.PasswordHash = _passwordHasher.HashPassword(
        user, password);

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

    private static string GetRequiredValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} không được để trống.");
        }

        return value.Trim();
    }

    private static string NormalizeEmail(string? email) => GetRequiredValue(email, "Email").ToLowerInvariant();

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidatePassword(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 8 ký tự, gồm chữ và số.");
    }


    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
{
    var identifier = GetRequiredValue(dto.LoginIdentifier, "LoginIdentifier");
    var password = GetRequiredValue(dto.Password, "Password");

    var normalizedEmail = identifier.ToLowerInvariant();
    var user = await _context.Users
        .Include(u => u.Role)
        .SingleOrDefaultAsync(u =>
            u.Username == identifier || u.Email == normalizedEmail);

    if (user is null || !user.IsActive)
    {
        throw new UnauthorizedAccessException(
            "Thông tin đăng nhập không hợp lệ.");
    }

    var verificationResult = _passwordHasher.VerifyHashedPassword(
        user,
        user.PasswordHash,
        password);

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

    public async Task<UserProfileDto?> GetProfileAsync(long userId)
    {
        var user = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == userId);
        return user is null ? null : ToProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        var email = NormalizeEmail(dto.Email);
        var phone = TrimToNull(dto.PhoneNumber);
        if (await _context.Users.AnyAsync(u => u.Id != userId && u.Email == email))
            throw new InvalidOperationException("Email đã được sử dụng.");
        if (phone is not null && await _context.Users.AnyAsync(u => u.Id != userId && u.PhoneNumber == phone))
            throw new InvalidOperationException("Số điện thoại đã được sử dụng.");
        user.Email = email;
        user.FullName = TrimToNull(dto.FullName);
        user.PhoneNumber = phone;
        await _context.SaveChangesAsync();
        return ToProfile(user);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        var current = GetRequiredValue(dto.CurrentPassword, "CurrentPassword");
        var next = GetRequiredValue(dto.NewPassword, "NewPassword");
        ValidatePassword(next);
        if (next != dto.ConfirmNewPassword) throw new InvalidOperationException("Xác nhận mật khẩu không khớp.");
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, current) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");
        user.PasswordHash = _passwordHasher.HashPassword(user, next);
        await _context.SaveChangesAsync();
    }

    private static UserProfileDto ToProfile(User user) => new()
    {
        Id = user.Id, Username = user.Username, Email = user.Email, FullName = user.FullName,
        PhoneNumber = user.PhoneNumber, RoleName = user.Role?.Name ?? string.Empty
    };
}
