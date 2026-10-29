using System.Data;
using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Orders;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class OrderService(KbsDbContext db) : IOrderService
{
    public async Task<OrderResponseDto> CreateAsync(
        long userId,
        CreateOrderDto dto,
        CancellationToken ct = default)
    {
        if (dto.OrderItems.Count == 0)
            throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.");
        if (dto.PaymentMethod is not ("COD" or "BANKING"))
            throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
        if (string.IsNullOrWhiteSpace(dto.CustomerName) || string.IsNullOrWhiteSpace(dto.PhoneNumber)
            || string.IsNullOrWhiteSpace(dto.ShippingAddress) || string.IsNullOrWhiteSpace(dto.PaymentMethod))
            throw new ArgumentException("Thiếu thông tin giao hàng hoặc thanh toán.");
        if (dto.OrderItems.Any(x => x.ProductId <= 0 || x.Quantity <= 0))
            throw new ArgumentException("Sản phẩm hoặc số lượng không hợp lệ.");

        var requested = dto.OrderItems.GroupBy(x => x.ProductId)
            .Select(x => new { ProductId = x.Key, Quantity = x.Sum(i => i.Quantity) }).ToList();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ids = requested.Select(x => x.ProductId).ToList();
        var products = await db.Products.Where(x => ids.Contains(x.Id) && x.IsActive)
            .Include(x => x.Inventory).Include(x => x.Images).ToListAsync(ct);
        if (products.Count != ids.Count)
            throw new InvalidOperationException("Một hoặc nhiều sản phẩm không tồn tại hoặc đã ngừng bán.");
        foreach (var item in requested)
        {
            var product = products.Single(x => x.Id == item.ProductId);
            if (product.Inventory is null || product.Inventory.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Sản phẩm '{product.Name}' không đủ tồn kho.");
        }

        var amount = requested.Sum(item => products.Single(product => product.Id == item.ProductId).Price * item.Quantity);
        if (dto.PaymentMethod == "BANKING" && (amount <= 0 || amount != decimal.Truncate(amount)))
            throw new ArgumentException("Thanh toán trực tuyến yêu cầu số tiền nguyên dương.");

        var order = new Order
        {
            UserId = userId,
            CustomerName = dto.CustomerName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            ShippingAddress = dto.ShippingAddress.Trim(),
            PaymentMethod = dto.PaymentMethod.Trim(),
            PaymentStatus = "PENDING",
            OrderStatus = "PENDING"
        };
        foreach (var item in requested)
        {
            var product = products.Single(x => x.Id == item.ProductId);
            var before = product.Inventory!.StockQuantity;
            product.Inventory!.StockQuantity -= item.Quantity;
            product.Inventory.UpdatedAt = DateTimeOffset.UtcNow;
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                Product = product
            });
            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id, ActorId = userId, Order = order,
                Delta = -item.Quantity, QuantityBefore = before,
                QuantityAfter = product.Inventory.StockQuantity, Reason = "Xuất kho theo đơn hàng"
            });
        }
        order.TotalPrice = order.OrderDetails.Sum(x => x.UnitPrice * x.Quantity);
        var cartItems = await db.CartItems
            .Where(x => x.UserId == userId && ids.Contains(x.ProductId)).ToListAsync(ct);
        foreach (var cartItem in cartItems)
        {
            var purchased = requested.Single(x => x.ProductId == cartItem.ProductId).Quantity;
            if (cartItem.Quantity <= purchased)
            {
                db.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity -= purchased;
                cartItem.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToDto(order);
    }

    public async Task<List<OrderResponseDto>> GetForUserAsync(long userId, CancellationToken ct = default)
    {
        var orders = await Query().AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<List<OrderResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await Query().AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<OrderResponseDto?> ConfirmReceiptAsync(
        long userId,
        long orderId,
        CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await Query().SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId, ct);
        if (order is null) return null;
        if (order.OrderStatus == "COMPLETED") return ToDto(order);
        if (order.OrderStatus != "SHIPPING")
            throw new InvalidOperationException("Chỉ có thể xác nhận nhận hàng khi đơn đang được giao.");
        order.OrderStatus = "COMPLETED";
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "40001" })
        {
            throw new InvalidOperationException("Đơn hàng vừa được cập nhật. Vui lòng tải lại và thử lại.", error);
        }
        catch (Npgsql.PostgresException error) when (error.SqlState == "40001")
        {
            throw new InvalidOperationException("Đơn hàng vừa được cập nhật. Vui lòng tải lại và thử lại.", error);
        }
        return ToDto(order);
    }

    public Task<OrderResponseDto?> CancelAsync(
        long userId,
        long orderId,
        CancellationToken ct = default) =>
        ChangeStatusAsync(orderId, "CANCELLED", userId, true, ct);

    public Task<OrderResponseDto?> UpdateStatusAsync(
        long id,
        UpdateOrderStatusDto dto,
        long actorId,
        CancellationToken ct = default) =>
        ChangeStatusAsync(id, dto.OrderStatus.Trim().ToUpperInvariant(), actorId, false, ct);

    private async Task<OrderResponseDto?> ChangeStatusAsync(
        long id,
        string status,
        long actorId,
        bool customerCancellation,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await Query().SingleOrDefaultAsync(
            x => x.Id == id && (!customerCancellation || x.UserId == actorId), ct);
        if (order is null) return null;
        if (customerCancellation && order.OrderStatus != "PENDING")
            throw new InvalidOperationException("Chỉ có thể hủy đơn hàng khi chưa được xác nhận.");
        ValidateTransition(order.OrderStatus, status);
        if (status == "CANCELLED")
        {
            foreach (var detail in order.OrderDetails)
            {
                var inventory = await db.Inventories.SingleAsync(x => x.ProductId == detail.ProductId, ct);
                var before = inventory.StockQuantity;
                if ((long)before + detail.Quantity > int.MaxValue)
                    throw new InvalidOperationException("Số lượng hoàn kho vượt giới hạn.");
                inventory.StockQuantity += detail.Quantity;
                inventory.UpdatedAt = DateTimeOffset.UtcNow;
                db.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = detail.ProductId, ActorId = actorId, OrderId = order.Id,
                    Delta = detail.Quantity, QuantityBefore = before,
                    QuantityAfter = inventory.StockQuantity, Reason = "Hoàn kho do hủy đơn hàng"
                });
            }
        }
        order.OrderStatus = status;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToDto(order);
    }

    internal static void ValidateTransition(string current, string next)
    {
        if (next is not ("PENDING" or "CONFIRMED" or "SHIPPING" or "COMPLETED" or "CANCELLED"))
            throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");
        var valid = (current, next) is ("PENDING", "CONFIRMED" or "CANCELLED")
            or ("CONFIRMED", "SHIPPING" or "CANCELLED") or ("SHIPPING", "COMPLETED");
        if (!valid) throw new InvalidOperationException($"Không thể chuyển từ {current} sang {next}.");
    }

    private IQueryable<Order> Query() => db.Orders
        .Include(x => x.OrderDetails).ThenInclude(x => x.Product).ThenInclude(x => x!.Images);

    private static OrderResponseDto ToDto(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        CustomerName = order.CustomerName,
        PhoneNumber = order.PhoneNumber,
        ShippingAddress = order.ShippingAddress,
        TotalPrice = order.TotalPrice,
        PaymentMethod = order.PaymentMethod,
        PaymentStatus = order.PaymentStatus,
        OrderStatus = order.OrderStatus,
        CreatedAt = order.CreatedAt,
        OrderDetails = order.OrderDetails.OrderBy(detail => detail.Id).Select(detail => new OrderDetailResponseDto
        {
            Id = detail.Id,
            ProductId = detail.ProductId,
            ProductName = detail.Product?.Name ?? string.Empty,
            ProductIsActive = detail.Product?.IsActive ?? false,
            ImageUrl = detail.Product?.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Id)
                .FirstOrDefault()?.ImageUrl,
            UnitPrice = detail.UnitPrice,
            Quantity = detail.Quantity
        }).ToList()
    };
}
