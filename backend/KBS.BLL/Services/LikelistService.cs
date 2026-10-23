using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Likelist;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KBS.BLL.Services;

public class LikelistService(KbsDbContext db) : ILikelistService
{
    public Task<List<LikelistItemResponseDto>> GetAsync(long userId, CancellationToken ct = default)
    {
        return db.Likelist.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new LikelistItemResponseDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product!.Name,
                Price = x.Product.Price,
                OriginalPrice = x.Product.OriginalPrice,
                BrandName = x.Product.Brand!.Name,
                CategoryName = x.Product.Category!.Name,
                IsActive = x.Product.IsActive,
                StockQuantity = x.Product.Inventory == null ? 0 : x.Product.Inventory.StockQuantity,
                CreatedAt = x.CreatedAt,
                Images = x.Product.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                    .Select(i => new ProductImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        AltText = i.AltText,
                        SortOrder = i.SortOrder
                    }).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<bool> AddAsync(
        long userId,
        long productId,
        CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsActive, ct))
        {
            return false;
        }

        if (await db.Likelist.AnyAsync(x => x.UserId == userId && x.ProductId == productId, ct))
        {
            return true;
        }

        var item = new LikelistItem { UserId = userId, ProductId = productId };
        db.Likelist.Add(item);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Likelist_UserId_ProductId"
        })
        {
            // Concurrent requests to like the same product have the same successful result.
            db.Entry(item).State = EntityState.Detached;
        }

        return true;
    }

    public async Task<bool> RemoveAsync(
        long userId,
        long productId,
        CancellationToken ct = default)
    {
        var item = await db.Likelist.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ProductId == productId, ct);
        if (item is null)
        {
            return false;
        }

        db.Likelist.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
