using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class DashboardService(KbsDbContext db) : IDashboardService
{
    public async Task<DashboardResponseDto> GetAsync(CancellationToken ct = default)
    {
        var since = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-6), TimeSpan.Zero);
        var orders = db.Orders.AsNoTracking();
        var list = await orders.Where(x => x.CreatedAt >= since).ToListAsync(ct);
        return new DashboardResponseDto
        {
            TotalOrders = await orders.CountAsync(ct),
            PendingOrders = await orders.CountAsync(x => x.OrderStatus == "PENDING", ct),
            TotalCustomers = await db.Users.CountAsync(x => x.Role!.Name == "Customer", ct),
            LowStockProducts = await db.Inventories.CountAsync(x => x.StockQuantity <= x.LowStockThreshold, ct),
            Revenue = await orders.Where(x => x.PaymentStatus == "PAID").SumAsync(x => (decimal?)x.TotalPrice, ct) ?? 0,
            RevenueLast7Days = list.GroupBy(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime.Date))
                .OrderBy(x => x.Key).Select(x => new RevenueByDayDto
                {
                    Date = x.Key,
                    Revenue = x.Where(o => o.PaymentStatus == "PAID").Sum(o => o.TotalPrice),
                    Orders = x.Count()
                }).ToList()
        };
    }
}
