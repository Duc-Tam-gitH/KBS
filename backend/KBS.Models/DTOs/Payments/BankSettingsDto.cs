using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Payments;

public record BankSettingsDto
{
    [Required]
    [StringLength(100)]
    public required string BankName { get; init; }
    [Required]
    [RegularExpression("^[0-9]{6}$")]
    public required string BankBin { get; init; }
    [Required]
    [RegularExpression("^[0-9]{6,19}$")]
    public required string AccountNumber { get; init; }
    [Required]
    [StringLength(150)]
    public required string AccountName { get; init; }
    [StringLength(100)]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "ID tài khoản SePay chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới.")]
    public string? SePayBankAccountId { get; init; }
}
