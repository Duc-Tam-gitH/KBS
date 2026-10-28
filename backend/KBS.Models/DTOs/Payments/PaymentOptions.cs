namespace KBS.Models.DTOs.Payments;

public class PaymentOptions
{
    public BankOptions Bank { get; set; } = new();
}

public class BankOptions
{
    public string BankBin { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}
