namespace KBS.Models.DTOs.Products;
public class ProductResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Description { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? SwitchType { get; set; }
    public string? Layout { get; set; }
    public string? ConnectionType { get; set; }
    public string? Keycap { get; set; }
    public string? Color { get; set; }
    public bool IsHotSwap { get; set; }
    public bool IsRGB { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
}
