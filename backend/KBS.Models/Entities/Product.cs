namespace KBS.Models.Entities;

public class Product
{
    public long Id { get; set; }

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

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Category? Category { get; set; }

    public Brand? Brand { get; set; }

    public Inventory? Inventory { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
