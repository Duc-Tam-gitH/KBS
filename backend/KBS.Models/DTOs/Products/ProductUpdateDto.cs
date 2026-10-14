namespace KBS.Models.DTOs.Products;

public class ProductUpdateDto
{
    public string Name { get; set; } = string.Empty;
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
    public bool IsActive { get; set; }
}
