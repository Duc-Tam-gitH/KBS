using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Shop;

public class SaveShopSettingsDto : IValidatableObject
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;
    [StringLength(100)]
    public string? Slogan { get; set; }
    [StringLength(2000)]
    public string? Description { get; set; }
    [Phone]
    [StringLength(30)]
    public string? Hotline { get; set; }
    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }
    [StringLength(500)]
    public string? WarehouseAddress { get; set; }
    [StringLength(100)]
    public string? BankName { get; set; }
    [RegularExpression("^[0-9]{6}$")]
    public string? BankBin { get; set; }
    [RegularExpression("^[0-9]{6,19}$")]
    public string? AccountNumber { get; set; }
    [StringLength(150)]
    public string? AccountName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var fields = new[] { BankName, BankBin, AccountNumber, AccountName };
        if (fields.Any(x => !string.IsNullOrWhiteSpace(x)) && fields.Any(string.IsNullOrWhiteSpace))
            yield return new ValidationResult("Nhập đủ tên ngân hàng, BIN, số tài khoản và tên chủ tài khoản.");
    }
}

public record PublicShopDto(
    string? Name,
    string? Slogan,
    string? Description,
    string? Hotline,
    string? Email,
    string? WarehouseAddress,
    string? LogoUrl,
    string? BannerUrl,
    DateTimeOffset? UpdatedAt);

public record AdminShopDto(
    PublicShopDto Shop,
    string? BankName,
    string? BankBin,
    string? AccountNumber,
    string? AccountName);
