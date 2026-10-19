using KBS.Models.DTOs.Users;

namespace KBS.BLL.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto?> UpdateStatusAsync(long id, bool isActive);
    Task<UserResponseDto?> UpdateRoleAsync(long id, long roleId);
}
