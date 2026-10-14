namespace KBS.Models.DTOs.Orders;

public class OrderResponseDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderDetailResponseDto> OrderDetails { get; set; } = new();
}
