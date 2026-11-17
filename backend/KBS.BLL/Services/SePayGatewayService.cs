using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KBS.BLL.Services;

public class SePayGatewayService(KbsDbContext db, IOptions<SePayGatewayOptions> settings)
{
    public SePayGatewayOptions Options => settings.Value;

    public bool Authenticate(string provided) => Options.Ready && provided.Length <= 256
        && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(Options.IpnSecret)));

    public static string Sign(Dictionary<string, string> fields, string secret)
    {
        string[] names = ["order_amount", "merchant", "currency", "operation", "order_description",
            "order_invoice_number", "customer_id", "payment_method", "success_url", "error_url", "cancel_url"];
        var text = string.Join(',', names.Where(x => fields.TryGetValue(x, out var value)
                && !string.IsNullOrEmpty(value) && value != "0").Select(x => $"{x}={fields[x]}"));
        return Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(text)));
    }

    public async Task<GatewayCheckoutDto> CreateAsync(long orderId, CancellationToken ct)
    {
        if (!Options.Ready) throw new InvalidOperationException("Chưa cấu hình đầy đủ SePay sandbox và IPN.");
        var order = await db.Orders.AsNoTracking().SingleAsync(x => x.Id == orderId, ct);
        if (order.PaymentMethod != "BANKING" || order.PaymentStatus != "PENDING"
            || order.OrderStatus == "CANCELLED" || order.TotalPrice <= 0
            || order.TotalPrice != decimal.Truncate(order.TotalPrice))
            throw new InvalidOperationException("Đơn không đủ điều kiện tạo thanh toán.");
        var payment = await db.GatewayPayments.SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
        if (payment == null)
        {
            payment = new GatewayPayment
            {
                OrderId = orderId, Amount = order.TotalPrice, MerchantId = Options.MerchantId,
                Environment = Options.Environment, Invoice = $"KBS{orderId}-{Guid.NewGuid():N}"
            };
            db.GatewayPayments.Add(payment);
            await db.SaveChangesAsync(ct);
        }
        if (payment.Amount != order.TotalPrice || payment.Status != "PENDING"
            || payment.MerchantId != Options.MerchantId || payment.Environment != Options.Environment)
            throw new InvalidOperationException("Thông tin thanh toán đã thay đổi; cần kiểm tra đơn hàng.");
        var callback = Options.PublicBaseUrl.TrimEnd('/') + "/api/payments/sepay/return";
        Dictionary<string, string> fields = new()
        {
            ["order_amount"] = payment.Amount.ToString("0", CultureInfo.InvariantCulture),
            ["merchant"] = Options.MerchantId, ["currency"] = "VND", ["operation"] = "PURCHASE",
            ["order_description"] = $"Thanh toan don hang KBS{orderId}",
            ["order_invoice_number"] = payment.Invoice, ["payment_method"] = "BANK_TRANSFER",
            ["success_url"] = callback + "?result=success", ["error_url"] = callback + "?result=error",
            ["cancel_url"] = callback + "?result=cancel"
        };
        fields["signature"] = Sign(fields, Options.SecretKey);
        return new GatewayCheckoutDto("https://pay-sandbox.sepay.vn/v1/checkout/init", fields, "sandbox");
    }

    public async Task<bool> ReceiveAsync(JsonElement payload, CancellationToken ct)
    {
        if (!Options.Ready) return false;
        var kind = Text(payload, "notification_type");
        if (kind is not ("ORDER_PAID" or "TRANSACTION_VOID")
            || !payload.TryGetProperty("order", out var inputOrder)
            || !payload.TryGetProperty("transaction", out var inputTransaction)) return false;
        var invoice = Text(inputOrder, "order_invoice_number");
        var transactionId = Text(inputTransaction, "id");
        if (!Guid.TryParse(transactionId, out var parsedId)
            || Text(inputOrder, "order_currency") != "VND"
            || Text(inputTransaction, "transaction_currency") != "VND"
            || Text(inputTransaction, "payment_method") != "BANK_TRANSFER"
            || !Amount(inputOrder, "order_amount", out var amount)
            || !Amount(inputTransaction, "transaction_amount", out var paid) || amount != paid || paid <= 0)
            return false;
        if (kind == "ORDER_PAID" && (Text(inputOrder, "order_status") != "CAPTURED"
            || Text(inputTransaction, "transaction_status") != "APPROVED"
            || Text(inputTransaction, "transaction_type") != "PAYMENT")) return false;
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var payment = await db.GatewayPayments.SingleOrDefaultAsync(x => x.Invoice == invoice, ct);
        if (payment == null || payment.Amount != paid || payment.Environment != Options.Environment
            || payment.MerchantId != Options.MerchantId) return false;
        transactionId = parsedId.ToString();
        if (await db.GatewayPayments.AnyAsync(x => x.OrderId != payment.OrderId
                && x.TransactionId == transactionId, ct)) return false;
        if (payment.TransactionId != null && payment.TransactionId != transactionId) return false;
        var order = await db.Orders.SingleAsync(x => x.Id == payment.OrderId, ct);
        if (order.TotalPrice != paid || order.PaymentMethod != "BANKING") return false;
        if (kind == "TRANSACTION_VOID")
        {
            if (payment.TransactionId != transactionId) return false;
            payment.Status = "VOID";
            order.PaymentStatus = "REVIEW";
        }
        else
        {
            if (payment.Status is "PAID" or "VOID" or "REVIEW") return true;
            payment.TransactionId = transactionId;
            payment.PaidAt = DateTimeOffset.UtcNow;
            payment.Status = order.OrderStatus == "CANCELLED" ? "REVIEW" : "PAID";
            order.PaymentStatus = payment.Status;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    private static string? Text(JsonElement value, string key) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(key, out var field) && field.ValueKind == JsonValueKind.String ? field.GetString() : null;

    private static bool Amount(JsonElement value, string key, out decimal amount)
    {
        amount = 0;
        if (!value.TryGetProperty(key, out var field)) return false;
        return field.ValueKind == JsonValueKind.Number ? field.TryGetDecimal(out amount)
            : decimal.TryParse(Text(value, key), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out amount);
    }
}
