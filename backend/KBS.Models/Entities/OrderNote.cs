namespace KBS.Models.Entities;

public class OrderNote
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public Order? Order { get; set; }
    public long StaffId { get; set; }
    public User? Staff { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
