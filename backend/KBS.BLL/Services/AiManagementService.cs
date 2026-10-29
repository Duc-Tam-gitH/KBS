using System.Data;
using KBS.DAL.Data;
using KBS.Models.DTOs.Ai;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class AiManagementService(KbsDbContext db)
{
    public async Task<List<AiModuleResponseDto>> ModulesAsync(CancellationToken ct)
    {
        var modules = await db.AiModules.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
        return modules.Select(ToDto).ToList();
    }

    public async Task<AiModuleResponseDto> EnableAsync(
        string id,
        bool enabled,
        long actorId,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var module = await LockedModule(id, ct);
        if (enabled && !module.IsIntegrated) throw new InvalidOperationException("AI này chưa được tích hợp.");
        if (module.IsEnabled != enabled)
        {
            module.IsEnabled = enabled;
            module.UpdatedAt = DateTimeOffset.UtcNow;
            db.AiEvents.Add(new AiEvent
            {
                ModuleId = id, ActorId = actorId, Kind = enabled ? "ENABLED" : "DISABLED",
                Message = enabled ? "Đã bật xử lý tự động." : "Đã tắt xử lý tự động."
            });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return ToDto(module);
    }

    public async Task<AiEventsPage> EventsAsync(
        string? moduleId,
        long? beforeId,
        int limit,
        CancellationToken ct)
    {
        if (limit is < 1 or > 100 || beforeId <= 0) throw new ArgumentException("Phân trang không hợp lệ.");
        if (moduleId != null && !await db.AiModules.AnyAsync(x => x.Id == moduleId, ct))
            throw new KeyNotFoundException("Không tìm thấy AI.");
        var items = await db.AiEvents.AsNoTracking()
            .Where(x => (moduleId == null || x.ModuleId == moduleId) && (beforeId == null || x.Id < beforeId))
            .OrderByDescending(x => x.Id).Take(limit + 1)
            .Select(x => new AiEventResponseDto(
                x.Id, x.ModuleId, x.Module.Name, x.OrderId, x.Kind, x.Message, x.CreatedAt)).ToListAsync(ct);
        var more = items.Count > limit;
        return new AiEventsPage(items.Take(limit).ToList(), more ? items[limit - 1].Id : null);
    }

    public async Task<AiPollDto> PollAsync(CancellationToken ct)
    {
        var module = await db.AiModules.SingleAsync(x => x.Id == "ai_auto", ct);
        module.LastHeartbeatAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        var orders = module.IsEnabled
            ? await db.Orders.AsNoTracking().Where(x => x.OrderStatus == "PENDING"
                && !db.AiEvents.Any(e => e.ModuleId == module.Id && e.OrderId == x.Id
                    && (e.Kind == "CONFIRMED" || e.Kind == "MANUAL_REVIEW")))
                .OrderBy(x => x.Id).Take(50).Select(x => new AiOrderDto(x.Id, x.TotalPrice)).ToListAsync(ct)
            : [];
        return new AiPollDto(module.IsEnabled, module.ApprovalLimit!.Value, orders);
    }

    public async Task<AiDecisionResponseDto> DecideAsync(
        long orderId,
        string decision,
        CancellationToken ct)
    {
        if (decision is not ("CONFIRM" or "MANUAL_REVIEW")) throw new ArgumentException("Quyết định không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var module = await LockedModule("ai_auto", ct);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        if (!module.IsEnabled || !module.IsIntegrated || order.OrderStatus != "PENDING")
            return new AiDecisionResponseDto(orderId, "SKIPPED");
        var previous = await db.AiEvents.FirstOrDefaultAsync(x => x.ModuleId == module.Id
            && x.OrderId == orderId && (x.Kind == "CONFIRMED" || x.Kind == "MANUAL_REVIEW"), ct);
        if (previous != null) return new AiDecisionResponseDto(orderId, previous.Kind);
        var confirm = order.TotalPrice < module.ApprovalLimit;
        if ((decision == "CONFIRM") != confirm)
            throw new InvalidOperationException("Quyết định không khớp tổng tiền hiện tại. Vui lòng đọc lại đơn hàng.");
        var result = confirm ? "CONFIRMED" : "MANUAL_REVIEW";
        if (confirm)
        {
            OrderService.ValidateTransition(order.OrderStatus, "CONFIRMED");
            order.OrderStatus = "CONFIRMED";
        }
        db.AiEvents.Add(new AiEvent
        {
            ModuleId = module.Id, OrderId = orderId, Kind = result,
            Message = confirm ? $"Đã tự động xác nhận đơn #{orderId}."
                : $"Đơn #{orderId} đạt ngưỡng duyệt thủ công, đang chờ staff xác nhận."
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new AiDecisionResponseDto(orderId, result);
    }

    private async Task<AiModule> LockedModule(string id, CancellationToken ct)
    {
        // Serialize toggles and decisions on the same module, including concurrent workers.
        var query = db.Database.IsRelational()
            ? db.AiModules.FromSqlInterpolated($"SELECT * FROM \"AiModules\" WHERE \"Id\" = {id} FOR UPDATE")
            : db.AiModules.Where(x => x.Id == id);
        return await query.SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Không tìm thấy AI.");
    }

    private static AiModuleResponseDto ToDto(AiModule module) => new(
        module.Id, module.Name, module.Description, module.SourceFile, module.IsIntegrated, module.IsEnabled,
        module.ApprovalLimit, module.LastHeartbeatAt, module.UpdatedAt,
        !module.IsIntegrated ? "NOT_INTEGRATED" : module.LastHeartbeatAt == null ? "NEVER_CONNECTED"
            : module.LastHeartbeatAt > DateTimeOffset.UtcNow.AddSeconds(-30) ? "ONLINE" : "OFFLINE");
}
