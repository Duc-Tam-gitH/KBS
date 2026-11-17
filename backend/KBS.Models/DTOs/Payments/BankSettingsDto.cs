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
    public Guid? SePayBankAccountId { get; init; }
}
