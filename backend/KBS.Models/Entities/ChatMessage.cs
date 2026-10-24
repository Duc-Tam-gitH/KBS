namespace KBS.Models.Entities;

public class ChatMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }
    public long SenderId { get; set; }
    public User? Sender { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
