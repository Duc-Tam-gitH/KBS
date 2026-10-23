using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Cart;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class CartService(KbsDbContext db) : ICartService
{
    public async Task<CartResponseDto> GetAsync(long userId, CancellationToken ct = default)
    {
        return ToDto(await Query(userId).ToListAsync(ct));
    }

    public async Task<CartResponseDto> AddAsync(
        long userId,
        AddCartItemDto dto,
        CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(x => x.Inventory)
            .SingleOrDefaultAsync(x => x.Id == dto.ProductId && x.IsActive, ct);

        if (product is null)
        {
            throw new ArgumentException("Sản phẩm không tồn tại hoặc đã ngừng bán.");
        }

        var item = await db.CartItems
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ProductId == dto.ProductId, ct);
        var quantity = (item?.Quantity ?? 0) + dto.Quantity;

        if (quantity > 100)
        {
            throw new ArgumentException("Số lượng trong giỏ không được vượt quá 100.");
        }

        if (product.Inventory is null || product.Inventory.StockQuantity < quantity)
        {
            throw new InvalidOperationException("Sản phẩm không đủ tồn kho.");
        }

        if (item is null)
        {
            db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            });
        }
        else
        {
            item.Quantity = quantity;
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartResponseDto?> UpdateAsync(
        long userId,
        long productId,
        UpdateCartItemDto dto,
        CancellationToken ct = default)
    {
        var item = await db.CartItems
            .Include(x => x.Product)
            .ThenInclude(x => x!.Inventory)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);

        if (item is null)
        {
            return null;
        }

        if (item.Product is null || !item.Product.IsActive)
        {
            throw new InvalidOperationException("Sản phẩm không còn được bán.");
        }

        if (item.Product.Inventory is null || item.Product.Inventory.StockQuantity < dto.Quantity)
        {
            throw new InvalidOperationException("Sản phẩm không đủ tồn kho.");
        }

        item.Quantity = dto.Quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<bool> RemoveAsync(
        long userId,
        long productId,
        CancellationToken ct = default)
    {
        var item = await db.CartItems
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);

        if (item is null)
        {
            return false;
        }

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<CartItem> Query(long userId)
    {
        return db.CartItems
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Product)
            .OrderBy(x => x.Id);
    }

    private static CartResponseDto ToDto(IEnumerable<CartItem> items)
    {
        return new CartResponseDto
        {
            Items = items
                .Where(x => x.Product is not null)
                .Select(x => new CartItemResponseDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product!.Name,
                    UnitPrice = x.Product.Price,
                    Quantity = x.Quantity
                })
                .ToList()
        };
    }
}
