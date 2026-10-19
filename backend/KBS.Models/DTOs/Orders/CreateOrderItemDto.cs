using System.ComponentModel.DataAnnotations;
namespace KBS.Models.DTOs.Orders;

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Sản phẩm không hợp lệ.")]
    public long ProductId { get; set; }
    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    public int Quantity { get; set; }
}
