using KBS.Models.DTOs.Cart;

namespace KBS.BLL.Interfaces;

public interface ICartService
{
    Task<CartResponseDto> GetAsync(long userId, CancellationToken ct = default);

    Task<CartResponseDto> AddAsync(long userId, AddCartItemDto dto, CancellationToken ct = default);

    Task<CartResponseDto?> UpdateAsync(
        long userId,
        long productId,
        UpdateCartItemDto dto,
        CancellationToken ct = default);

    Task<bool> RemoveAsync(long userId, long productId, CancellationToken ct = default);
}
