using KBS.Models.DTOs.Users;

namespace KBS.BLL.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponseDto>> GetAllAsync();
    Task<List<UserRoleDto>> RolesAsync();
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);
    Task<UserResponseDto> SaveAsync(
        long id,
        SaveUserDto dto,
        long actorId);
    Task DeleteAsync(long id, long actorId);
    Task<UserResponseDto> UpdateStatusAsync(
        long id,
        bool isActive,
        long actorId);
    Task<UserResponseDto> UpdateRoleAsync(
        long id,
        long roleId,
        long actorId);
}
