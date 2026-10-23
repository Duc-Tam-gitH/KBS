using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Cart;

public class UpdateCartItemDto
{
    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    public int Quantity { get; set; }
}
