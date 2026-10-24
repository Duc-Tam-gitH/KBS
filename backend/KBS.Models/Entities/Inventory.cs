namespace KBS.Models.Entities;

public class Inventory
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product? Product { get; set; }
}
