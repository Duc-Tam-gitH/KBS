using System.ComponentModel.DataAnnotations;
namespace KBS.Models.DTOs.Products;

public class ProductCreateDto
{
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên sản phẩm dài từ 2 đến 200 ký tự.")]
    public string Name { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá sản phẩm phải hợp lệ.")]
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Description { get; set; }
    public long CategoryId { get; set; }
    public long BrandId { get; set; }
    public string? SwitchType { get; set; }
    public string? Layout { get; set; }
    public string? ConnectionType { get; set; }
    public string? Keycap { get; set; }
    public string? Color { get; set; }
    public bool IsHotSwap { get; set; }
    public bool IsRGB { get; set; }
    [MaxLength(10, ErrorMessage = "Tối đa 10 ảnh cho một sản phẩm.")]
    public List<ProductImageDto> Images { get; set; } = new();
}
