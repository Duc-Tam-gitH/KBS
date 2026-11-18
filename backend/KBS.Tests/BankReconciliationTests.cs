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
    [InlineData(9999999, true, true)]
    [InlineData(10000000, true, false)]
    [InlineData(10000001, true, false)]
    [InlineData(10000, false, false)]
    public async Task Reconciled_bank_payment_reaches_customer_and_enabled_ai(
        decimal amount, bool enabled, bool confirmed)
    {
        await using var db = new KbsDbContext(new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        db.AiModules.Add(new AiModule
        {
            Id = "ai_auto", IsEnabled = enabled, IsIntegrated = true, ApprovalLimit = 10000000
        });
        db.Orders.Add(new Order { Id = 7, UserId = 1, PaymentMethod = "BANKING", TotalPrice = amount });
        db.BankPayments.Add(new BankPayment
        {
            OrderId = 7, Amount = amount, SePayBankAccountId = "ba_Test123",
            AccountNumber = "123456789", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        // An old manual-review decision must not permanently exclude a newly paid small order.
        db.AiEvents.Add(new AiEvent { ModuleId = "ai_auto", OrderId = 7, Kind = "MANUAL_REVIEW" });
        await db.SaveChangesAsync();
        var ai = new AiManagementService(db);
        Assert.Empty((await ai.PollAsync(default)).Orders);
        Assert.True(await new BankReconciliationService(db).ApplyAsync(new BankTransactionDto(
            "txn_Test123", "ba_Test123", "123456789", amount, "Thanh toan KBS7", DateTimeOffset.UtcNow), default));
        var batch = await ai.PollAsync(default);
        Assert.Equal(confirmed ? 1 : 0, batch.Orders.Count);
        foreach (var order in batch.Orders)
            await ai.DecideAsync(order.Id, "CONFIRM", default);
        var status = await new PaymentService(db).GetAsync(1, 7, default);
        Assert.Equal("PAID", status.PaymentStatus);
        Assert.Equal(confirmed ? "CONFIRMED" : "PENDING", status.OrderStatus);
        Assert.Empty((await ai.PollAsync(default)).Orders);
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("opaque", true)]
    [InlineData("empty-id", false)]
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
        var bankId = scenario == "opaque" ? "ba_TestAccount123" : Guid.NewGuid().ToString();
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
        var dto = new BankTransactionDto(scenario switch
            { "opaque" => "txn_Test123", "empty-id" => "", _ => Guid.NewGuid().ToString() },
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
