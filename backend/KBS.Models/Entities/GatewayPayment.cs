namespace KBS.Models.Entities;

public class GatewayPayment
{
    public long OrderId { get; set; }
    public string Invoice { get; set; } = "";
    public string Environment { get; set; } = "sandbox";
    public string MerchantId { get; set; } = "";
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
}
