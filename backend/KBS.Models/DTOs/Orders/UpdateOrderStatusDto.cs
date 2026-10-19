using System.ComponentModel.DataAnnotations;
namespace KBS.Models.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [Required(ErrorMessage = "Trạng thái đơn hàng là bắt buộc.")]
    [RegularExpression("^(PENDING|CONFIRMED|SHIPPING|COMPLETED|CANCELLED)$", ErrorMessage = "Trạng thái đơn hàng không hợp lệ.")]
    public string OrderStatus { get; set; } = string.Empty;
}
