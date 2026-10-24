namespace KBS.Models.Entities;

public class ProductReview
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Product? Product { get; set; }
    public User? User { get; set; }
}
