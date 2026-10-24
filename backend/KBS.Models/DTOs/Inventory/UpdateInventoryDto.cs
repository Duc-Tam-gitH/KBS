using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Inventory;

public class UpdateInventoryDto
{
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
