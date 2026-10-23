using KBS.Models.DTOs.Likelist;

namespace KBS.BLL.Interfaces;

public interface ILikelistService
{
    Task<List<LikelistItemResponseDto>> GetAsync(long userId, CancellationToken ct = default);

    Task<bool> AddAsync(
        long userId,
        long productId,
        CancellationToken ct = default);

    Task<bool> RemoveAsync(
        long userId,
        long productId,
        CancellationToken ct = default);
}
