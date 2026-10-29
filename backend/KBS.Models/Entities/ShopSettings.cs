using System.ComponentModel.DataAnnotations;

namespace KBS.Models.Entities;

public class ShopSettings
{
    public int Id { get; set; } = 1;
    [MaxLength(150)]
    public string? Name { get; set; }
    [MaxLength(100)]
    public string? Slogan { get; set; }
    [MaxLength(2000)]
    public string? Description { get; set; }
    [MaxLength(30)]
    public string? Hotline { get; set; }
    [MaxLength(254)]
    public string? Email { get; set; }
    [MaxLength(500)]
    public string? WarehouseAddress { get; set; }
    [MaxLength(100)]
    public string? BankName { get; set; }
    [MaxLength(6)]
    public string? BankBin { get; set; }
    [MaxLength(19)]
    public string? AccountNumber { get; set; }
    [MaxLength(150)]
    public string? AccountName { get; set; }
    public byte[]? LogoData { get; set; }
    public byte[]? BannerData { get; set; }
    [MaxLength(20)]
    public string? LogoContentType { get; set; }
    [MaxLength(20)]
    public string? BannerContentType { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
