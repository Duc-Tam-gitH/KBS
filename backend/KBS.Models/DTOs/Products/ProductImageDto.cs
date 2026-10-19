using System.ComponentModel.DataAnnotations;
namespace KBS.Models.DTOs.Products;
public class ProductImageDto
{
    public long Id { get; set; }
    [Required(ErrorMessage = "URL ảnh là bắt buộc.")]
    [Url(ErrorMessage = "URL ảnh không hợp lệ.")]
    [StringLength(2048)] public string ImageUrl { get; set; } = string.Empty;
    [StringLength(200)] public string? AltText { get; set; }
    [Range(0, 100)] public int SortOrder { get; set; }
}
