namespace KBS.Models.Entities;

public class CartItem
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }

    public Product? Product { get; set; }
}
