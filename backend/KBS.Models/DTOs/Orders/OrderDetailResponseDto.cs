namespace KBS.Models.DTOs.Orders;

public class OrderDetailResponseDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool ProductIsActive { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
