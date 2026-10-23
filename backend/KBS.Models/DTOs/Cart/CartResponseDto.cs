namespace KBS.Models.DTOs.Cart;

public class CartResponseDto
{
    public List<CartItemResponseDto> Items { get; set; } = [];

    public decimal TotalPrice => Items.Sum(x => x.Subtotal);
}
