namespace KBS.Models.DTOs.Cart;

public class CartItemResponseDto
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }
    public string? Layout { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}
