using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KBS.Tests;

public class SePayGatewayTests
{
    private static SePayGatewayOptions Settings() => new()
    {
        Enabled = true, MerchantId = "SP-TEST-FIXTURE", SecretKey = "spsk_test_fixture",
        IpnSecret = new string('x', 40), PublicBaseUrl = "https://payments.example.test"
    };

    [Fact]
    public void Signing_uses_documented_field_order_and_ignores_unsigned_fields()
    {
        Dictionary<string, string> a = new()
        {
            ["currency"] = "VND", ["merchant"] = "TEST", ["order_amount"] = "10000", ["lang"] = "vi"
        };
        Dictionary<string, string> b = new()
        {
            ["order_amount"] = "10000", ["merchant"] = "TEST", ["currency"] = "VND", ["customer_id"] = "0"
        };
        Assert.Equal(SePayGatewayService.Sign(a, "secret"), SePayGatewayService.Sign(b, "secret"));
        b["order_amount"] = "10001";
        Assert.NotEqual(SePayGatewayService.Sign(a, "secret"), SePayGatewayService.Sign(b, "secret"));
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("amount", false)]
    [InlineData("currency", false)]
    [InlineData("status", false)]
    [InlineData("invoice", false)]
    [InlineData("cancelled", true)]
    public async Task Ipn_validates_payment_and_is_idempotent(string scenario, bool accepted)
    {
        await using var db = new KbsDbContext(new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var order = new Order { Id = 7, UserId = 1, TotalPrice = 10000000, PaymentMethod = "BANKING" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var service = new SePayGatewayService(db, Options.Create(Settings()));
        Assert.False(service.Authenticate("wrong"));
        Assert.True(service.Authenticate(new string('x', 40)));
        var checkout = await service.CreateAsync(7, default);
        Assert.Equal("10000000", checkout.Fields["order_amount"]);
        Assert.DoesNotContain(Settings().SecretKey, JsonSerializer.Serialize(checkout));
        if (scenario == "cancelled") { order.OrderStatus = "CANCELLED"; await db.SaveChangesAsync(); }
        var payload = JsonSerializer.SerializeToElement(new
        {
            notification_type = "ORDER_PAID",
            order = new
            {
                order_invoice_number = scenario == "invoice" ? "wrong" : checkout.Fields["order_invoice_number"],
                order_currency = "VND", order_amount = "10000000", order_status = "CAPTURED"
            },
            transaction = new
            {
                id = Guid.NewGuid().ToString(), payment_method = "BANK_TRANSFER", transaction_type = "PAYMENT",
                transaction_amount = scenario == "amount" ? "9999999" : "10000000",
                transaction_currency = scenario == "currency" ? "USD" : "VND",
                transaction_status = scenario == "status" ? "FAILED" : "APPROVED"
            }
        });
        Assert.Equal(accepted, await service.ReceiveAsync(payload, default));
        Assert.Equal(accepted, await service.ReceiveAsync(payload, default));
        Assert.Equal(!accepted ? "PENDING" : scenario == "cancelled" ? "REVIEW" : "PAID", order.PaymentStatus);
        Assert.Equal(scenario == "cancelled" ? "CANCELLED" : "PENDING", order.OrderStatus);
        Assert.Single(await db.GatewayPayments.ToListAsync());
    }

    [Fact]
    public async Task Ipn_endpoint_rejects_missing_secret_and_does_not_trust_return_url()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/payments/sepay/ipn", new { notification_type = "ORDER_PAID" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var returned = await client.GetAsync("/api/payments/sepay/return?result=success");
        returned.EnsureSuccessStatusCode();
        using var scope = app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<KbsDbContext>().GatewayPayments.ToListAsync());
    }
}
