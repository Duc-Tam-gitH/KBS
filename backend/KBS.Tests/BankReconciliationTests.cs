using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KBS.Tests;

public class BankReconciliationTests
{
    [Theory]
    [InlineData("valid", true)]
    [InlineData("amount", false)]
    [InlineData("account", false)]
    [InlineData("bank", false)]
    [InlineData("old", false)]
    [InlineData("cancelled", false)]
    [InlineData("ambiguous", false)]
    [InlineData("prefix", false)]
    public async Task Only_exact_verified_payment_is_applied_once(string scenario, bool expected)
    {
        await using var db = new KbsDbContext(new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var bankId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = 7, UserId = 1, PaymentMethod = "BANKING", TotalPrice = 125000,
            OrderStatus = scenario == "cancelled" ? "CANCELLED" : "PENDING"
        };
        db.Orders.Add(order);
        db.BankPayments.Add(new BankPayment
        {
            OrderId = 7, Amount = 125000, SePayBankAccountId = bankId,
            AccountNumber = "123456789", CreatedAt = now.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var dto = new BankTransactionDto(Guid.NewGuid().ToString(),
            scenario == "bank" ? Guid.NewGuid().ToString() : bankId,
            scenario == "account" ? "987654321" : "123456789",
            scenario == "amount" ? 124999 : 125000,
            scenario switch { "ambiguous" => "KBS7 KBS8", "prefix" => "KBS70", _ => "Thanh toan KBS7" },
            scenario == "old" ? now.AddDays(-1) : now);
        var service = new BankReconciliationService(db);
        Assert.Equal(expected, await service.ApplyAsync(dto, default));
        Assert.Equal(expected ? "PAID" : "PENDING", order.PaymentStatus);
        Assert.Equal(scenario == "cancelled" ? "CANCELLED" : "PENDING", order.OrderStatus);
        Assert.False(await service.ApplyAsync(dto, default));
    }
}
