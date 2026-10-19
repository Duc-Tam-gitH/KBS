namespace KBS.Models.DTOs.Dashboard;
public class DashboardResponseDto { public int TotalOrders { get; set; } public int PendingOrders { get; set; } public int TotalCustomers { get; set; } public int LowStockProducts { get; set; } public decimal Revenue { get; set; } public List<RevenueByDayDto> RevenueLast7Days { get; set; } = new(); }
public class RevenueByDayDto { public DateOnly Date { get; set; } public decimal Revenue { get; set; } public int Orders { get; set; } }
