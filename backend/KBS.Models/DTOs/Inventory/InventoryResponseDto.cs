namespace KBS.Models.DTOs.Inventory;

public class InventoryResponseDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int StockQuantity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
