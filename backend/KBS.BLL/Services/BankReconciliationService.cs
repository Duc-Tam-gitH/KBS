using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public partial class BankReconciliationService(KbsDbContext db)
{
    [GeneratedRegex(@"(?<![A-Z0-9])KBS([1-9][0-9]*)(?![A-Z0-9])", RegexOptions.IgnoreCase)]
    private static partial Regex OrderCode();

    public async Task<bool> ApplyAsync(BankTransactionDto dto, CancellationToken ct)
    {
        var matches = OrderCode().Matches(dto.Content);
        if (matches.Count != 1 || !Guid.TryParse(dto.Id, out var transactionId)
            || !Guid.TryParse(dto.BankAccountId, out var bankId)
            || !long.TryParse(matches[0].Groups[1].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var orderId)) return false;
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var payment = await db.BankPayments.SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
        if (payment == null || payment.TransactionId != null || payment.Amount != dto.Amount || dto.Amount <= 0
            || payment.AccountNumber != dto.AccountNumber || payment.SePayBankAccountId != bankId.ToString()
            || dto.OccurredAt < payment.CreatedAt.AddSeconds(-1) || dto.OccurredAt > DateTimeOffset.UtcNow.AddMinutes(1)
            || await db.BankPayments.AnyAsync(x => x.TransactionId == transactionId.ToString(), ct)) return false;
        var order = await db.Orders.SingleAsync(x => x.Id == orderId, ct);
        if (order.PaymentMethod != "BANKING" || order.PaymentStatus != "PENDING"
            || order.OrderStatus == "CANCELLED" || order.TotalPrice != dto.Amount) return false;
        order.PaymentStatus = "PAID";
        payment.TransactionId = transactionId.ToString();
        payment.PaidAt = dto.OccurredAt;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
