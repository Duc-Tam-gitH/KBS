using System.ComponentModel.DataAnnotations;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.API.Services;

public static class BankPaymentEndpoints
{
    public static void MapBankPayments(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/payments").RequireAuthorization(p => p.RequireRole("Admin"));
        group.MapGet("", async (KbsDbContext db, IConfiguration config, CancellationToken ct) =>
        {
            var bank = await db.ShopSettings.AsNoTracking().Select(x => new
            {
                x.BankName, x.BankBin, x.AccountNumber, x.AccountName, x.SePayBankAccountId
            }).SingleOrDefaultAsync(ct);
            var payments = await db.BankPayments.AsNoTracking().OrderByDescending(x => x.CreatedAt)
                .Take(50).Select(x => new { x.OrderId, x.Amount, x.AccountNumber, x.TransactionId, x.PaidAt })
                .ToListAsync(ct);
            return TypedResults.Ok(new
            {
                bank, payments, tokenConfigured = !string.IsNullOrWhiteSpace(config["Payments:SePay:ApiToken"]),
                gatewayEnabled = config.GetValue<bool>("Payments:Gateway:Enabled"),
                ipnUrl = (config["Payments:Gateway:PublicBaseUrl"] ?? "").TrimEnd('/') + "/api/payments/sepay/ipn",
                gatewayPayments = await db.GatewayPayments.AsNoTracking().OrderByDescending(x => x.CreatedAt)
                    .Take(50).Select(x => new { x.OrderId, x.Amount, x.Status, x.Environment, x.PaidAt }).ToListAsync(ct)
            });
        });
        group.MapPut("/bank", async (BankSettingsDto dto, KbsDbContext db, CancellationToken ct) =>
        {
            List<ValidationResult> errors = [];
            if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["bank"] = errors.Select(x => x.ErrorMessage ?? "Dữ liệu không hợp lệ.").ToArray()
                });
            var bank = await db.ShopSettings.SingleOrDefaultAsync(ct);
            if (bank == null)
            {
                bank = new ShopSettings();
                db.ShopSettings.Add(bank);
            }
            bank.BankName = dto.BankName.Trim();
            bank.BankBin = dto.BankBin;
            bank.AccountNumber = dto.AccountNumber;
            bank.AccountName = dto.AccountName.Trim();
            bank.SePayBankAccountId = string.IsNullOrEmpty(dto.SePayBankAccountId) ? null : dto.SePayBankAccountId;
            bank.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
