using System.Data;
using KBS.DAL.Data;
using KBS.Models.Entities;
using KBS.Models.DTOs.Staff;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class ChatService(KbsDbContext db)
{
    public async Task<List<ChatConversationDto>> ListAsync(
        long userId,
        string role,
        CancellationToken ct)
    {
        var query = db.ChatConversations.AsNoTracking();
        query = role == "Admin" ? query : role == "Staff"
            ? query.Where(x => x.StaffId == userId || x.StaffId == null)
            : query.Where(x => x.CustomerId == userId);
        return await query.OrderByDescending(x => x.Messages.Max(m => (long?)m.Id) ?? 0)
            .Select(x => new ChatConversationDto
            {
                Id = x.Id, CustomerId = x.CustomerId, CustomerName = x.Customer!.FullName ?? x.Customer.Username,
                StaffId = x.StaffId, StaffName = x.Staff == null ? null : x.Staff.FullName ?? x.Staff.Username,
                LastMessage = x.Messages.OrderByDescending(m => m.Id).Select(m => m.Content).FirstOrDefault(),
                LastMessageAt = x.Messages.OrderByDescending(m => m.Id)
                    .Select(m => (DateTimeOffset?)m.CreatedAt).FirstOrDefault(),
                Unread = x.Messages.Count(m => m.ReadAt == null
                    && (role == "Customer" ? m.SenderId != x.CustomerId : m.SenderId == x.CustomerId))
            }).ToListAsync(ct);
    }

    public async Task<long> OpenAsync(
        long userId,
        string role,
        long? customerId,
        CancellationToken ct)
    {
        var id = role == "Customer" ? userId : customerId
            ?? throw new ArgumentException("Chọn khách hàng để mở hội thoại.");
        if (role == "Customer" && customerId.HasValue && customerId != userId)
            throw new UnauthorizedAccessException("Không có quyền mở hội thoại này.");
        if (!await db.Users.AnyAsync(x => x.Id == id && x.Role!.Name == "Customer" && x.IsActive, ct))
            throw new KeyNotFoundException("Khách hàng không tồn tại hoặc đã bị khóa.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var conversation = await db.ChatConversations.SingleOrDefaultAsync(x => x.CustomerId == id, ct);
        if (conversation is null)
        {
            conversation = new ChatConversation { CustomerId = id, StaffId = role == "Customer" ? null : userId };
            db.ChatConversations.Add(conversation);
        }
        else if (role != "Customer")
        {
            if (conversation.StaffId != null && conversation.StaffId != userId && role != "Admin")
                throw new InvalidOperationException("Khách hàng đang được nhân viên khác hỗ trợ.");
            conversation.StaffId ??= userId;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return conversation.Id;
    }

    public async Task ClaimAsync(long id, long userId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var conversation = await db.ChatConversations.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy hội thoại.");
        if (conversation.StaffId != null && conversation.StaffId != userId)
            throw new InvalidOperationException("Hội thoại đã được nhân viên khác tiếp nhận.");
        conversation.StaffId = userId;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task<ChatConversation> AuthorizedAsync(
        long id,
        long userId,
        string role,
        CancellationToken ct)
    {
        var conversation = await db.ChatConversations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy hội thoại.");
        if (role == "Admin" || (role == "Staff" && conversation.StaffId == userId)
            || (role == "Customer" && conversation.CustomerId == userId)) return conversation;
        throw new UnauthorizedAccessException("Không có quyền truy cập hội thoại này.");
    }

    public async Task<List<ChatMessageDto>> MessagesAsync(
        long id,
        long userId,
        string role,
        long? beforeId,
        long? afterId,
        CancellationToken ct)
    {
        await AuthorizedAsync(id, userId, role, ct);
        var query = db.ChatMessages.AsNoTracking().Where(x => x.ConversationId == id);
        if (beforeId.HasValue) query = query.Where(x => x.Id < beforeId);
        if (afterId.HasValue) query = query.Where(x => x.Id > afterId);
        var ordered = afterId.HasValue ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
        var messages = await ordered.Take(100)
            .Select(x => new ChatMessageDto
            {
                Id = x.Id, SenderId = x.SenderId, SenderName = x.Sender!.FullName ?? x.Sender.Username,
                Content = x.Content, CreatedAt = x.CreatedAt, ReadAt = x.ReadAt
            }).ToListAsync(ct);
        if (!afterId.HasValue) messages.Reverse();
        return messages;
    }

    public async Task SendAsync(
        long id,
        long userId,
        string role,
        string content,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 4000)
            throw new ArgumentException("Tin nhắn phải có nội dung, tối đa 4000 ký tự.");
        var conversation = await AuthorizedAsync(id, userId, role, ct);
        if (!await db.Users.AnyAsync(x => x.Id == conversation.CustomerId && x.IsActive, ct))
            throw new InvalidOperationException("Tài khoản khách hàng đã bị khóa.");
        db.ChatMessages.Add(new ChatMessage { ConversationId = id, SenderId = userId, Content = content.Trim() });
        await db.SaveChangesAsync(ct);
    }

    public async Task ReadAsync(
        long id,
        long userId,
        string role,
        long throughId,
        CancellationToken ct)
    {
        var conversation = await AuthorizedAsync(id, userId, role, ct);
        var unread = await db.ChatMessages.Where(x => x.ConversationId == id && x.ReadAt == null
            && x.Id <= throughId && (role == "Customer"
                ? x.SenderId != conversation.CustomerId : x.SenderId == conversation.CustomerId)).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var message in unread) message.ReadAt = now;
        await db.SaveChangesAsync(ct);
    }
}
