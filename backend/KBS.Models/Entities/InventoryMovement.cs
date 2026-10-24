namespace KBS.Models.Entities;

public class InventoryMovement
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public Product? Product { get; set; }
    public long ActorId { get; set; }
    public User? Actor { get; set; }
    public long? OrderId { get; set; }
    public Order? Order { get; set; }
    public int Delta { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
