using System.Data;
using KBS.DAL.Data;
using KBS.Models.DTOs.Staff;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class StaffService(KbsDbContext db)
{
    public async Task<StaffSummaryDto> SummaryAsync(long staffId, CancellationToken ct)
    {
        var unread = await db.ChatMessages.CountAsync(x =>
            x.Conversation!.StaffId == staffId && x.SenderId == x.Conversation.CustomerId
            && x.ReadAt == null, ct);
        return new StaffSummaryDto
        {
            NewOrdersCount = await db.Orders.CountAsync(x => x.OrderStatus == "PENDING", ct),
            ProcessingOrdersCount = await db.Orders.CountAsync(x => x.OrderStatus == "CONFIRMED", ct),
            LowStockCount = await db.Inventories.CountAsync(x => x.StockQuantity <= x.LowStockThreshold, ct),
            UnreadMessagesCount = unread,
            UnassignedConversations = await db.ChatConversations.CountAsync(x => x.StaffId == null, ct),
            RecentMovements = await db.InventoryMovements.AsNoTracking()
                .Where(x => x.ActorId == staffId).OrderByDescending(x => x.Id).Take(5)
                .Select(x => new StaffActivityDto
                {
                    Id = x.Id, ProductName = x.Product!.Name, Delta = x.Delta,
                    Reason = x.Reason, CreatedAt = x.CreatedAt
                }).ToListAsync(ct)
        };
    }

    public async Task<List<StaffInventoryDto>> InventoryAsync(CancellationToken ct) =>
        await db.Inventories.AsNoTracking()
        .OrderBy(x => x.StockQuantity).ThenBy(x => x.ProductId)
        .Select(x => new StaffInventoryDto
        {
            ProductId = x.ProductId, Name = x.Product!.Name, Description = x.Product.Description,
            CategoryId = x.Product.CategoryId, CategoryName = x.Product.Category!.Name,
            OrderedQuantity = x.Product.OrderDetails.Where(d => d.Order!.OrderStatus == "PENDING"
                || d.Order.OrderStatus == "CONFIRMED" || d.Order.OrderStatus == "SHIPPING")
                .Sum(d => (long?)d.Quantity) ?? 0,
            Price = x.Product.Price, IsActive = x.Product.IsActive,
            StockQuantity = x.StockQuantity, LowStockThreshold = x.LowStockThreshold, UpdatedAt = x.UpdatedAt
        }).ToListAsync(ct);

    public async Task<List<InventoryMovementDto>> MovementsAsync(long productId, CancellationToken ct) =>
        await db.InventoryMovements.AsNoTracking().Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.Id).Select(x => new InventoryMovementDto
            {
                Id = x.Id, ActorId = x.ActorId, ActorName = x.Actor!.FullName ?? x.Actor.Username,
                OrderId = x.OrderId, Delta = x.Delta, QuantityBefore = x.QuantityBefore,
                QuantityAfter = x.QuantityAfter, Reason = x.Reason, CreatedAt = x.CreatedAt
            }).ToListAsync(ct);

    public async Task AdjustStockAsync(
        long productId,
        long actorId,
        StockAdjustmentRequest dto,
        CancellationToken ct)
    {
        if (dto.Delta == 0 || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500)
            throw new ArgumentException("Nhập số lượng thay đổi khác 0 và lý do tối đa 500 ký tự.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var inventory = await db.Inventories.SingleOrDefaultAsync(x => x.ProductId == productId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tồn kho.");
        if (inventory.StockQuantity != dto.ExpectedQuantity)
            throw new InvalidOperationException("Tồn kho đã thay đổi. Vui lòng tải lại trước khi cập nhật.");
        var after = (long)inventory.StockQuantity + dto.Delta;
        if (after < 0 || after > int.MaxValue) throw new ArgumentException("Số lượng tồn kho không hợp lệ.");
        db.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = productId, ActorId = actorId, Delta = dto.Delta,
            QuantityBefore = inventory.StockQuantity, QuantityAfter = (int)after, Reason = dto.Reason.Trim()
        });
        inventory.StockQuantity = (int)after;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ThresholdAsync(long productId, int threshold, CancellationToken ct)
    {
        if (threshold < 0) throw new ArgumentException("Ngưỡng cảnh báo không được âm.");
        var inventory = await db.Inventories.SingleOrDefaultAsync(x => x.ProductId == productId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tồn kho.");
        inventory.LowStockThreshold = threshold;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<StaffCustomerDto>> CustomersAsync(CancellationToken ct) => await db.Users.AsNoTracking()
        .Where(x => x.Role!.Name == "Customer").OrderBy(x => x.Id)
        .Select(x => new StaffCustomerDto
        {
            Id = x.Id, Username = x.Username, FullName = x.FullName, Email = x.Email,
            PhoneNumber = x.PhoneNumber, CreatedAt = x.CreatedAt, IsActive = x.IsActive,
            TotalSpent = x.Orders.Where(o => o.OrderStatus == "COMPLETED").Sum(o => (decimal?)o.TotalPrice) ?? 0,
            OrderCount = x.Orders.Count
        }).ToListAsync(ct);

    public async Task EnsureCustomerAsync(long id, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == id && x.Role!.Name == "Customer", ct))
            throw new KeyNotFoundException("Không tìm thấy khách hàng.");
    }

    public async Task SetCustomerStatusAsync(long id, bool active, CancellationToken ct)
    {
        var customer = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.Role!.Name == "Customer", ct)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");
        customer.IsActive = active;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<StaffReviewDto>> CustomerReviewsAsync(long id, CancellationToken ct)
    {
        await EnsureCustomerAsync(id, ct);
        return await db.ProductReviews.AsNoTracking().Where(x => x.UserId == id)
            .OrderByDescending(x => x.Id)
            .Select(x => new StaffReviewDto
            {
                Id = x.Id, ProductId = x.ProductId, Rating = x.Rating, Comment = x.Comment, CreatedAt = x.CreatedAt
            }).ToListAsync(ct);
    }

    public async Task<OrderOperationsDto> OrderOperationsAsync(long id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        var notes = await db.OrderNotes.AsNoTracking().Where(x => x.OrderId == id).OrderByDescending(x => x.Id)
            .Select(x => new OrderNoteDto
            {
                Id = x.Id, Content = x.Content, CreatedAt = x.CreatedAt,
                StaffName = x.Staff!.FullName ?? x.Staff.Username
            })
            .ToListAsync(ct);
        return new OrderOperationsDto { TrackingNumber = order.TrackingNumber, Notes = notes };
    }

    public async Task TrackingAsync(long id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        if (!string.IsNullOrWhiteSpace(order.TrackingNumber)) return;
        // MOCK DATA — mã tạm, sẽ thay bằng mã từ đơn vị vận chuyển.
        order.TrackingNumber = $"TEMP-{order.Id}-{Guid.NewGuid():N}";
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task AddNoteAsync(
        long id,
        long actorId,
        string content,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            throw new ArgumentException("Ghi chú phải có nội dung, tối đa 2000 ký tự.");
        if (!await db.Orders.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        db.OrderNotes.Add(new OrderNote { OrderId = id, StaffId = actorId, Content = content.Trim() });
        await db.SaveChangesAsync(ct);
    }
}
