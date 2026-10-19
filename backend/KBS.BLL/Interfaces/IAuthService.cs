using KBS.Models.DTOs.Authentication;

namespace KBS.BLL.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserProfileDto?> GetProfileAsync(long userId);
    Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(long userId, ChangePasswordDto dto);
}
