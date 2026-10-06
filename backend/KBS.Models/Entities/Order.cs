namespace KBS.Models.Entities;

public class Order
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    // Trạng thái thanh toán: PENDING (chờ xử lý), PAID (đã thanh toán), CANCELLED (đã hủy)
    public string PaymentStatus { get; set; } = "PENDING";

    public string OrderStatus { get; set; } = "PENDING";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
