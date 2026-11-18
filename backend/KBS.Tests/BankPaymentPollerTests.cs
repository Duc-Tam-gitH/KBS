using System.Net;
using KBS.API.Services;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KBS.Tests;

public class BankPaymentPollerTests
{
    [Fact]
    public async Task Poll_sends_full_vietnam_datetime_and_applies_paginated_incoming_payment()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<KbsDbContext>(options => options.UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<BankReconciliationService>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        db.Orders.Add(new Order { Id = 7, UserId = 1, PaymentMethod = "BANKING", TotalPrice = 10000 });
        db.BankPayments.Add(new BankPayment
        {
            OrderId = 7, Amount = 10000, AccountNumber = "123456789", SePayBankAccountId = "ba_test",
            CreatedAt = DateTimeOffset.Parse("2026-01-01T18:30:00Z")
        });
        await db.SaveChangesAsync();
        using var handler = new SePayHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://userapi.sepay.vn/v2/") };
        var poller = new BankPaymentPoller(provider.GetRequiredService<IServiceScopeFactory>(),
            new ClientFactory(client), new ConfigurationBuilder().Build(), NullLogger<BankPaymentPoller>.Instance);
        await poller.PollAsync("test-token", default);
        Assert.Equal(2, handler.Requests);
        db.ChangeTracker.Clear();
        Assert.Equal("PAID", (await db.Orders.SingleAsync()).PaymentStatus);
        Assert.Equal("txn_test", (await db.BankPayments.SingleAsync()).TransactionId);
    }

    private sealed class ClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class SePayHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            var query = Uri.UnescapeDataString(request.RequestUri!.Query);
            Assert.Contains("transaction_date_from=2026-01-02 00:00:00", query);
            Assert.Contains($"page={Requests}", query);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            var body = Requests == 1
                ? """{"status":"success","data":[],"meta":{"pagination":{"has_more":true}}}"""
                : """{"status":"success","data":[{"id":"txn_test","bank_account_id":"ba_test","account_number":"123456789","transfer_type":"in","amount_in":10000,"amount_out":0,"transaction_content":"KBS7","transaction_date":"2026-01-02 02:00:00"}],"meta":{"pagination":{"has_more":false}}}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
