namespace KBS.Models.DTOs.Orders;

public class CreateOrderDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}
