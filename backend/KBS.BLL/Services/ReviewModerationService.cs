using System.Data;
using KBS.DAL.Data;
using KBS.Models.DTOs.Ai;
using KBS.Models.DTOs.Common;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class ReviewModerationService(KbsDbContext db)
{
    public async Task<ReviewPoll> PollAsync(CancellationToken ct)
    {
        var module = await db.AiModules.SingleAsync(x => x.Id == "ai_reviewnpl", ct);
        module.LastHeartbeatAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        var enabled = module.IsEnabled && module.IsIntegrated;
        var reviews = enabled ? await db.ProductReviews.AsNoTracking()
            .Where(x => x.ModerationStatus == "PENDING").OrderBy(x => x.Id).Take(50)
            .Select(x => new PendingReview(x.Id, x.Comment)).ToListAsync(ct) : [];
        return new ReviewPoll(enabled, reviews);
    }

    public Task<ReviewDecisionResult> WorkerAsync(
        long id,
        ReviewWorkerDecision dto,
        CancellationToken ct)
    {
        if (dto.Decision is not ("APPROVED" or "FLAGGED") || dto.MatchedKeywords == null
            || dto.MatchedKeywords.Length > 50
            || dto.MatchedKeywords.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100))
            throw new ArgumentException("Kết quả kiểm tra không hợp lệ.");
        var reason = string.Join(", ", dto.MatchedKeywords.Distinct());
        if (reason.Length > 1000 || (dto.Decision == "FLAGGED") != (reason.Length > 0))
            throw new ArgumentException("Từ khóa không khớp kết quả kiểm tra.");
        return ApplyAsync(id, dto.Decision, reason.Length == 0 ? null : reason, null, ct);
    }

    public Task<ReviewDecisionResult> ModerateAsync(
        long id,
        string status,
        long actorId,
        CancellationToken ct)
    {
        if (status is not ("APPROVED" or "HIDDEN")) throw new ArgumentException("Trạng thái không hợp lệ.");
        return ApplyAsync(id, status, null, actorId, ct);
    }

    private async Task<ReviewDecisionResult> ApplyAsync(
        long id,
        string status,
        string? reason,
        long? actorId,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Same module lock as ON/OFF; staff and workers cannot overwrite each other concurrently.
        var modules = db.Database.IsRelational()
            ? db.AiModules.FromSqlRaw("SELECT * FROM \"AiModules\" WHERE \"Id\" = 'ai_reviewnpl' FOR UPDATE")
            : db.AiModules.Where(x => x.Id == "ai_reviewnpl");
        var module = await modules.SingleAsync(ct);
        var review = await db.ProductReviews.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đánh giá.");
        if (actorId == null && (!module.IsEnabled || !module.IsIntegrated))
            throw new InvalidOperationException("AI đã tắt.");
        if ((actorId == null && review.ModerationStatus != "PENDING") || review.ModerationStatus == status)
            return new ReviewDecisionResult(id, review.ModerationStatus);
        review.ModerationStatus = status;
        review.CheckedAt = DateTimeOffset.UtcNow;
        if (actorId == null) review.ModerationReason = reason;
        db.AiEvents.Add(new AiEvent
        {
            ModuleId = module.Id, ActorId = actorId, Kind = "REVIEW_" + status,
            Message = $"Đánh giá #{id}: {status}. " + (actorId.HasValue ? "Nhân viên đã duyệt." : "AI đã kiểm tra.")
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ReviewDecisionResult(id, status);
    }

    public async Task<PagedResponse<ModerationReview>> ListAsync(
        string? status,
        int page,
        CancellationToken ct)
    {
        if (status != null && status is not ("PENDING" or "APPROVED" or "FLAGGED" or "HIDDEN"))
            throw new ArgumentException("Trạng thái không hợp lệ.");
        if (page < 1 || page > 1000000) throw new ArgumentException("Trang không hợp lệ.");
        const int size = 20;
        var query = db.ProductReviews.AsNoTracking().Where(x => status == null || x.ModerationStatus == status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new ModerationReview(x.Id, x.ProductId, x.Product!.Name,
                x.User!.FullName ?? x.User.Username, x.Rating, x.Comment, x.ModerationStatus,
                x.ModerationReason, x.CreatedAt, x.CheckedAt)).ToListAsync(ct);
        return new PagedResponse<ModerationReview>
        {
            Items = items, Page = page, PageSize = size, TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
