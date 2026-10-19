using System.ComponentModel.DataAnnotations;
namespace KBS.Models.DTOs.Orders;

public class CreateOrderDto
{
    [Required(ErrorMessage = "Tên người nhận là bắt buộc.")]
    [StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression("^[0-9+ ()-]{8,20}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc.")]
    [StringLength(500)] public string ShippingAddress { get; set; } = string.Empty;
    [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc.")]
    [RegularExpression("^(COD|BANKING)$", ErrorMessage = "Phương thức thanh toán chỉ nhận COD hoặc BANKING.")]
    public string PaymentMethod { get; set; } = string.Empty;
    [MinLength(1, ErrorMessage = "Giỏ hàng đang trống.")]
    [MaxLength(50)]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}
