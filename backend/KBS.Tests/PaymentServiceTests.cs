using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace KBS.Tests;

public class PaymentServiceTests
{
    private static PaymentOptions Settings() => new()
    {
        Bank = new BankOptions
        {
            BankBin = "970422", BankName = "MBBank", AccountNumber = "1234567890", AccountName = "TEST ACCOUNT"
        }
    };

    private static KbsDbContext NewDb() => new(new DbContextOptionsBuilder<KbsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static PaymentService Service(KbsDbContext db, PaymentOptions? settings = null)
    {
        var options = Options.Create(settings ?? Settings());
        return new PaymentService(db, options);
    }

    [Fact]
    public void Only_cod_and_banking_are_supported()
    {
        using var db = NewDb();
        var service = Service(db);
        service.ValidateMethod("COD");
        service.ValidateMethod("BANKING");
        Assert.Throws<ArgumentException>(() => service.ValidateMethod("WALLET"));
        var dto = new KBS.Models.DTOs.Orders.CreateOrderDto { PaymentMethod = "WALLET" };
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto)
        {
            MemberName = nameof(dto.PaymentMethod)
        };
        Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(
            dto.PaymentMethod, context, null));
    }

    [Fact]
    public async Task Bank_qr_uses_owned_order_amount_and_manual_confirmation_is_audited_once()
    {
        await using var db = NewDb();
        var order = new Order { UserId = 1, TotalPrice = 125000, PaymentMethod = "BANKING" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var service = Service(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(2, order.Id, default));
        var payment = await service.CreateAsync(1, order.Id, default);
        Assert.Equal(125000, payment.Amount);
        Assert.Contains("amount=125000", payment.QrImageUrl);
        Assert.Contains($"addInfo=KBS{order.Id}", payment.QrImageUrl);
        Assert.Contains("970422-1234567890", payment.QrImageUrl);
        Assert.Equal("PENDING", order.PaymentStatus);
        await service.ConfirmBankAsync(9, order.Id, default);
        await service.ConfirmBankAsync(9, order.Id, default);
        Assert.Equal("PAID", order.PaymentStatus);
        var note = Assert.Single(await db.OrderNotes.ToListAsync());
        Assert.Equal(9, note.StaffId);
        Assert.Contains("125000", note.Content);
        Assert.Empty(db.InventoryMovements);
        order.PaymentMethod = "COD";
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmBankAsync(9, order.Id, default));
    }

    [Fact]
    public async Task Cancelled_order_cannot_create_payment_or_be_manually_confirmed()
    {
        await using var db = NewDb();
        var order = new Order { UserId = 1, TotalPrice = 10000, PaymentMethod = "BANKING", OrderStatus = "CANCELLED" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var service = Service(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(1, order.Id, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmBankAsync(9, order.Id, default));
        Assert.Equal("PENDING", order.PaymentStatus);
    }

}
