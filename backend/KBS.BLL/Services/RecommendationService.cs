using KBS.DAL.Data;
using KBS.Models.DTOs.Ai;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public record RecommendationSnapshot(
    DateTimeOffset Version, DateTimeOffset CreatedAt, long[] Random, Dictionary<long, long[]> Customers);

public class RecommendationCache
{
    public RecommendationSnapshot? Current;
}

public class RecommendationService(KbsDbContext db, RecommendationCache cache)
{
    public async Task<RecommendationPoll> PollAsync(CancellationToken ct)
    {
        var module = await db.AiModules.SingleAsync(x => x.Id == "ai_recommender", ct);
        module.LastHeartbeatAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        var enabled = module.IsEnabled && module.IsIntegrated;
        var products = enabled
            ? await Available().OrderBy(x => x.Id).Select(x => new RecommendationProduct(
                x.Id, x.CategoryId, x.BrandId, x.Price, x.Sound, x.SwitchType, x.Layout)).ToListAsync(ct)
            : [];
        return new RecommendationPoll(enabled, module.UpdatedAt, products,
            enabled ? await HistoriesAsync(ct) : []);
    }

    public async Task PublishAsync(RecommendationUpdate dto, CancellationToken ct)
    {
        var module = await db.AiModules.AsNoTracking().SingleAsync(x => x.Id == "ai_recommender", ct);
        if (!module.IsEnabled || !module.IsIntegrated || module.UpdatedAt != dto.Version)
            throw new InvalidOperationException("Cấu hình AI đã thay đổi. Vui lòng đọc lại.");
        var available = (await Available().Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        var histories = (await HistoriesAsync(ct)).ToDictionary(x => x.UserId);
        bool Valid(long[] ids) => ids.Length <= 8 && ids.Distinct().Count() == ids.Length
            && ids.All(available.Contains);
        if (!Valid(dto.Random) || dto.Customers.Select(x => x.UserId).Distinct().Count() != dto.Customers.Count
            || dto.Customers.Any(x => !Valid(x.ProductIds) || !histories.ContainsKey(x.UserId)
                || histories[x.UserId].Products.Any(p => x.ProductIds.Contains(p.Id))))
            throw new InvalidOperationException("Danh sách gợi ý hoặc lịch sử mua đã thay đổi. Vui lòng đọc lại.");
        var customers = dto.Customers.ToDictionary(x => x.UserId, x => x.ProductIds.ToArray());
        var previous = Volatile.Read(ref cache.Current);
        if (previous == null || previous.Version != dto.Version || previous.Customers.Count != customers.Count
            || customers.Any(x => !previous.Customers.TryGetValue(x.Key, out var ids)
                || !ids.SequenceEqual(x.Value)))
        {
            db.AiEvents.Add(new AiEvent
            {
                ModuleId = module.Id, Kind = "RECOMMENDED",
                Message = $"Đã cập nhật gợi ý theo lịch sử cho {customers.Count} khách hàng và gợi ý ngẫu nhiên."
            });
            await db.SaveChangesAsync(ct);
        }
        Volatile.Write(ref cache.Current, new RecommendationSnapshot(
            dto.Version, DateTimeOffset.UtcNow, dto.Random.ToArray(), customers));
    }

    public async Task<RecommendationPage> GetAsync(long? userId, CancellationToken ct)
    {
        var module = await db.AiModules.AsNoTracking().SingleAsync(x => x.Id == "ai_recommender", ct);
        if (!module.IsEnabled || !module.IsIntegrated) return new RecommendationPage("DISABLED", []);
        var snapshot = Volatile.Read(ref cache.Current);
        if (snapshot == null || snapshot.Version != module.UpdatedAt
            || snapshot.CreatedAt < DateTimeOffset.UtcNow.AddSeconds(-30))
            return new RecommendationPage("WAITING", []);
        var history = userId.HasValue ? (await HistoriesAsync(ct, userId)).FirstOrDefault() : null;
        var personalized = history != null;
        if (personalized && !snapshot.Customers.ContainsKey(userId!.Value))
            return new RecommendationPage("WAITING", [], "PERSONALIZED");
        var ids = personalized ? snapshot.Customers[userId!.Value] : snapshot.Random;
        var bought = history?.Products.Select(x => x.Id).ToHashSet() ?? [];
        var products = await Available().Where(x => ids.Contains(x.Id))
            .Include(x => x.Category).Include(x => x.Brand).Include(x => x.Images).Include(x => x.Inventory)
            .ToListAsync(ct);
        return new RecommendationPage("READY", products.Where(x => !bought.Contains(x.Id))
            .OrderBy(x => Array.IndexOf(ids, x.Id)).Select(ProductService.ToDto).ToList(),
            personalized ? "PERSONALIZED" : "RANDOM");
    }

    private async Task<List<RecommendationHistory>> HistoriesAsync(CancellationToken ct, long? userId = null)
    {
        var orders = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == "COMPLETED" && (userId == null || x.UserId == userId))
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.UserId,
                Products = x.OrderDetails.Where(d => d.Product != null).OrderBy(d => d.Id)
                    .Select(d => new RecommendationProduct(d.Product!.Id, d.Product.CategoryId,
                        d.Product.BrandId, d.Product.Price, d.Product.Sound, d.Product.SwitchType, d.Product.Layout))
                    .ToList()
            }).ToListAsync(ct);
        return orders.GroupBy(x => x.UserId)
            .Select(group => new RecommendationHistory(group.Key,
                group.Take(5).SelectMany(x => x.Products).DistinctBy(x => x.Id).ToList()))
            .Where(x => x.Products.Count > 0).ToList();
    }

    private IQueryable<Product> Available() => db.Products.AsNoTracking()
        .Where(x => x.IsActive && x.Inventory != null && x.Inventory.StockQuantity > 0);
}
