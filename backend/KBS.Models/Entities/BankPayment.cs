namespace KBS.Models.Entities;

public class BankPayment
{
    public long OrderId { get; set; }
    public decimal Amount { get; set; }
    public string BankBin { get; set; } = "";
    public string? SePayBankAccountId { get; set; }
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? TransactionId { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
