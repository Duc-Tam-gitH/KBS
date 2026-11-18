using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using Microsoft.EntityFrameworkCore;

namespace KBS.API.Services;

public class BankPaymentPoller(
    IServiceScopeFactory scopes,
    IHttpClientFactory clients,
    IConfiguration config,
    ILogger<BankPaymentPoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        var warnedMissingToken = false;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var token = config["Payments:SePay:ApiToken"];
            if (config.GetValue<bool>("Payments:Gateway:Enabled")) continue;
            if (string.IsNullOrWhiteSpace(token))
            {
                if (!warnedMissingToken)
                    logger.LogWarning("Bank reconciliation is disabled: configure Payments:SePay:ApiToken with a SePay Live API token. Bank orders remain pending until verified.");
                warnedMissingToken = true;
                continue;
            }
            warnedMissingToken = false;
            try
            {
                await PollAsync(token, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (HttpRequestException error)
            {
                logger.LogWarning("SePay transaction request failed (HTTP {StatusCode}); retry in 30 seconds.",
                    error.StatusCode is { } status ? (int)status : null);
            }
            catch (Exception error)
            {
                // Never log bank response bodies, account details or authorization headers.
                logger.LogWarning("Bank reconciliation failed ({ErrorType}); retry in 30 seconds.",
                    error.GetType().Name);
            }
        }
    }

    public async Task PollAsync(string token, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var from = await (from p in db.BankPayments.AsNoTracking()
            join o in db.Orders.AsNoTracking() on p.OrderId equals o.Id
            where p.SePayBankAccountId != null && p.TransactionId == null
                && o.PaymentStatus == "PENDING" && o.OrderStatus != "CANCELLED"
            select (DateTimeOffset?)p.CreatedAt).MinAsync(ct);
        if (from == null) return;
        var service = scope.ServiceProvider.GetRequiredService<BankReconciliationService>();
        using var client = clients.CreateClient("SePay");
        var date = Uri.EscapeDataString(from.Value.ToOffset(TimeSpan.FromHours(7))
            .ToString("yyyy-MM-dd' 00:00:00'", CultureInfo.InvariantCulture));
        var received = 0;
        var applied = 0;
        for (var page = 1; ; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"transactions?transfer_type=in&transaction_date_from={date}&per_page=100&page={page}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct),
                cancellationToken: ct);
            var root = json.RootElement;
            if (root.GetProperty("status").GetString() != "success")
                throw new InvalidOperationException("Bank API unsuccessful.");
            foreach (var row in root.GetProperty("data").EnumerateArray())
            {
                if (row.GetProperty("transfer_type").GetString() != "in"
                    || row.GetProperty("amount_out").GetDecimal() != 0) continue;
                var time = DateTime.ParseExact(row.GetProperty("transaction_date").GetString()!,
                    "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);
                received++;
                if (await service.ApplyAsync(new BankTransactionDto(
                    row.GetProperty("id").GetString()!, row.GetProperty("bank_account_id").GetString()!,
                    row.GetProperty("account_number").GetString()!, row.GetProperty("amount_in").GetDecimal(),
                    row.GetProperty("transaction_content").GetString() ?? "",
                    new DateTimeOffset(time, TimeSpan.FromHours(7))), ct)) applied++;
            }
            if (!root.GetProperty("meta").GetProperty("pagination").GetProperty("has_more").GetBoolean()) break;
            await Task.Delay(TimeSpan.FromMilliseconds(400), ct);
        }
        logger.LogInformation("SePay reconciliation: {Received} incoming transactions checked, {Applied} orders paid.",
            received, applied);
    }
}
