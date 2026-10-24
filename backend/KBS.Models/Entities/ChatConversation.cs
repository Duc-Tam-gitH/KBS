namespace KBS.Models.Entities;

public class ChatConversation
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public User? Customer { get; set; }
    public long? StaffId { get; set; }
    public User? Staff { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
