using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Inventory;

public class UpdateInventoryDto
{
    [Required(ErrorMessage = "StockQuantity là bắt buộc")]
    [Range(0, int.MaxValue, ErrorMessage = "StockQuantity phải lớn hơn hoặc bằng 0")]
    public int StockQuantity { get; set; }
}