namespace KBS.Models.DTOs.Orders;

public class CreateOrderItemDto
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}
