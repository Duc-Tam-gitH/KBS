using System.Data;
using KBS.BLL.Interfaces; using KBS.DAL.Data; using KBS.Models.DTOs.Orders; using KBS.Models.Entities; using Microsoft.EntityFrameworkCore;
namespace KBS.BLL.Services;
public class OrderService(KbsDbContext db) : IOrderService
{
    public async Task<OrderResponseDto> CreateAsync(long userId, CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto.OrderItems.Count == 0) throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.");
        if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.ShippingAddress) || string.IsNullOrWhiteSpace(dto.PaymentMethod)) throw new ArgumentException("Thiếu thông tin giao hàng hoặc thanh toán.");
        if (dto.OrderItems.Any(x => x.ProductId <= 0 || x.Quantity <= 0)) throw new ArgumentException("Sản phẩm hoặc số lượng không hợp lệ.");
        var requested = dto.OrderItems.GroupBy(x => x.ProductId).Select(x => new { ProductId=x.Key, Quantity=x.Sum(i=>i.Quantity) }).ToList();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ids = requested.Select(x => x.ProductId).ToList();
        var products = await db.Products.Where(x => ids.Contains(x.Id) && x.IsActive).Include(x => x.Inventory).ToListAsync(ct);
        if (products.Count != ids.Count) throw new InvalidOperationException("Một hoặc nhiều sản phẩm không tồn tại hoặc đã ngừng bán.");
        foreach (var item in requested) { var p=products.Single(x=>x.Id==item.ProductId); if (p.Inventory is null || p.Inventory.StockQuantity < item.Quantity) throw new InvalidOperationException($"Sản phẩm '{p.Name}' không đủ tồn kho."); }
        var order = new Order { UserId=userId, CustomerName=dto.CustomerName.Trim(), PhoneNumber=dto.PhoneNumber.Trim(), ShippingAddress=dto.ShippingAddress.Trim(), PaymentMethod=dto.PaymentMethod.Trim(), PaymentStatus="PENDING", OrderStatus="PENDING" };
        foreach (var item in requested) { var p=products.Single(x=>x.Id==item.ProductId); p.Inventory!.StockQuantity -= item.Quantity; p.Inventory.UpdatedAt=DateTimeOffset.UtcNow; order.OrderDetails.Add(new OrderDetail { ProductId=p.Id, UnitPrice=p.Price, Quantity=item.Quantity, Product=p }); }
        order.TotalPrice = order.OrderDetails.Sum(x => x.UnitPrice * x.Quantity);
        db.Orders.Add(order); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return ToDto(order);
    }
    public async Task<List<OrderResponseDto>> GetForUserAsync(long userId,CancellationToken ct=default) { var orders = await Query().Where(x=>x.UserId==userId).OrderByDescending(x=>x.CreatedAt).ToListAsync(ct); return orders.Select(ToDto).ToList(); }
    public async Task<List<OrderResponseDto>> GetAllAsync(CancellationToken ct=default) { var orders = await Query().OrderByDescending(x=>x.CreatedAt).ToListAsync(ct); return orders.Select(ToDto).ToList(); }
    public async Task<OrderResponseDto?> UpdateStatusAsync(long id,UpdateOrderStatusDto dto,CancellationToken ct=default){var status=dto.OrderStatus.Trim().ToUpperInvariant();var allowed=new Dictionary<string,string[]>{{"PENDING",["CONFIRMED","CANCELLED"]},{"CONFIRMED",["SHIPPING","CANCELLED"]},{"SHIPPING",["COMPLETED"]},{"COMPLETED",[]},{"CANCELLED",[]}};if(!allowed.TryGetValue(status,out _))throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");var o=await Query().SingleOrDefaultAsync(x=>x.Id==id,ct);if(o is null)return null;if(!allowed.TryGetValue(o.OrderStatus,out var next)||!next.Contains(status))throw new InvalidOperationException($"Không thể chuyển từ {o.OrderStatus} sang {status}.");if(status=="CANCELLED"){foreach(var d in o.OrderDetails){var inventory=await db.Inventories.SingleAsync(x=>x.ProductId==d.ProductId,ct);inventory.StockQuantity+=d.Quantity;inventory.UpdatedAt=DateTimeOffset.UtcNow;}}o.OrderStatus=status;await db.SaveChangesAsync(ct);return ToDto(o);}
    private IQueryable<Order> Query()=>db.Orders.Include(x=>x.OrderDetails).ThenInclude(x=>x.Product);
    private static OrderResponseDto ToDto(Order o)=>new(){Id=o.Id,UserId=o.UserId,CustomerName=o.CustomerName,PhoneNumber=o.PhoneNumber,ShippingAddress=o.ShippingAddress,TotalPrice=o.TotalPrice,PaymentMethod=o.PaymentMethod,PaymentStatus=o.PaymentStatus,OrderStatus=o.OrderStatus,CreatedAt=o.CreatedAt,OrderDetails=o.OrderDetails.Select(d=>new OrderDetailResponseDto{Id=d.Id,ProductId=d.ProductId,ProductName=d.Product?.Name??string.Empty,UnitPrice=d.UnitPrice,Quantity=d.Quantity}).ToList()};
}
