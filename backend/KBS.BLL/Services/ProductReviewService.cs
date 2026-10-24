using KBS.DAL.Data;
using KBS.Models.DTOs.Common;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class ProductReviewService(KbsDbContext db)
{
    public async Task<ProductReviewsResponseDto?> GetAsync(
        long productId,
        int? rating,
        int page,
        CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsActive, ct)) return null;
        var query = db.ProductReviews.AsNoTracking().Where(x => x.ProductId == productId);
        var counts = await query.GroupBy(x => x.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() }).ToListAsync(ct);
        var total = counts.Sum(x => x.Count);
        if (rating.HasValue) query = query.Where(x => x.Rating == rating);
        var filteredTotal = await query.CountAsync(ct);
        page = Math.Max(1, page);
        const int pageSize = 10;
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductReviewResponseDto
            {
                Id = x.Id,
                DisplayName = x.User!.FullName ?? x.User.Username,
                Rating = x.Rating,
                Comment = x.Comment,
                CreatedAt = x.CreatedAt
            }).ToListAsync(ct);
        return new ProductReviewsResponseDto
        {
            TotalReviews = total,
            AverageRating = total == 0 ? 0 : counts.Sum(x => x.Rating * (double)x.Count) / total,
            RatingCounts = counts.ToDictionary(x => x.Rating, x => x.Count),
            Reviews = new PagedResponse<ProductReviewResponseDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = filteredTotal,
                TotalPages = (int)Math.Ceiling(filteredTotal / (double)pageSize)
            }
        };
    }

    public async Task<bool> CreateAsync(
        long productId,
        long userId,
        ProductReviewCreateDto dto,
        CancellationToken ct = default)
    {
        if (dto.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(dto.Comment) || dto.Comment.Length > 2000)
            throw new ArgumentException("Chọn từ 1 đến 5 sao và nhập nhận xét tối đa 2000 ký tự.");
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsActive, ct)) return false;
        var purchased = await db.OrderDetails.AnyAsync(x => x.ProductId == productId
            && x.Order!.UserId == userId && x.Order.OrderStatus == "COMPLETED", ct);
        if (!purchased)
            throw new InvalidOperationException("Bạn chỉ có thể đánh giá sản phẩm trong đơn hàng đã hoàn tất.");
        if (await db.ProductReviews.AnyAsync(x => x.ProductId == productId && x.UserId == userId, ct))
            throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này.");
        db.ProductReviews.Add(new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim()
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này.", error);
        }
        return true;
    }
}
