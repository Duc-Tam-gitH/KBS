using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class PaymentService(KbsDbContext db)
{
    private Task<BankOptions?> BankAsync(CancellationToken ct) => db.ShopSettings.AsNoTracking()
        .Select(x => new BankOptions
        {
            BankBin = x.BankBin ?? "", BankName = x.BankName ?? "",
            AccountNumber = x.AccountNumber ?? "", AccountName = x.AccountName ?? ""
        }).SingleOrDefaultAsync(ct);

    private static bool Configured(BankOptions? bank) => bank != null
        && Regex.IsMatch(bank.BankBin, @"^[0-9]{6}$")
        && Regex.IsMatch(bank.AccountNumber, @"^[0-9]{6,19}$")
        && !string.IsNullOrWhiteSpace(bank.AccountName) && !string.IsNullOrWhiteSpace(bank.BankName);

    public async Task<bool> BankAvailableAsync(CancellationToken ct) => Configured(await BankAsync(ct));

    public async Task ValidateMethodAsync(string method, CancellationToken ct)
    {
        if (method == "BANKING" && !await BankAvailableAsync(ct))
            throw new InvalidOperationException("Chưa cấu hình tài khoản nhận chuyển khoản.");
        if (method is not ("COD" or "BANKING"))
            throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
    }

    public async Task<PaymentResponseDto> GetAsync(
        long userId,
        long orderId,
        CancellationToken ct)
        => ToDto(await FindOwned(userId, orderId, ct));

    public async Task<PaymentResponseDto> CreateAsync(
        long userId,
        long orderId,
        CancellationToken ct)
    {
        var order = await FindOwned(userId, orderId, ct);
        var result = ToDto(order);
        if (order.PaymentStatus == "PAID") return result;
        if (order.OrderStatus == "CANCELLED" || order.PaymentStatus != "PENDING")
            throw new InvalidOperationException("Đơn hàng không còn chờ thanh toán.");
        if (order.PaymentMethod is not ("COD" or "BANKING"))
            throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
        if (order.PaymentMethod == "BANKING")
        {
            if (order.TotalPrice <= 0 || order.TotalPrice != decimal.Truncate(order.TotalPrice))
                throw new InvalidOperationException("QR chuyển khoản yêu cầu số tiền nguyên dương.");
            var bank = await BankAsync(ct);
            if (!Configured(bank)) throw new InvalidOperationException("Chưa cấu hình tài khoản nhận chuyển khoản.");
            result.TransferContent = $"KBS{order.Id}";
            result.BankName = bank!.BankName;
            result.AccountNumber = bank.AccountNumber;
            result.AccountName = bank.AccountName;
            result.QrImageUrl = $"https://img.vietqr.io/image/{bank.BankBin}-{bank.AccountNumber}-compact2.svg"
                + $"?amount={order.TotalPrice.ToString("0", CultureInfo.InvariantCulture)}"
                + $"&addInfo={Uri.EscapeDataString(result.TransferContent)}"
                + $"&accountName={Uri.EscapeDataString(bank.AccountName)}";
        }
        return result;
    }

    public async Task<PaymentResponseDto> ConfirmBankAsync(
        long actorId,
        long orderId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        if (order.PaymentMethod != "BANKING" || order.OrderStatus == "CANCELLED")
            throw new InvalidOperationException("Chỉ xác nhận chuyển khoản cho đơn ngân hàng chưa bị hủy.");
        if (order.PaymentStatus == "PAID") return ToDto(order);
        if (order.PaymentStatus != "PENDING")
            throw new InvalidOperationException("Đơn hàng không còn chờ thanh toán.");
        order.PaymentStatus = "PAID";
        db.OrderNotes.Add(new OrderNote
        {
            OrderId = order.Id, StaffId = actorId,
            Content = $"Xác nhận đã nhận đủ chuyển khoản {order.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)}"
                + $" VND cho KBS{order.Id} sau khi đối chiếu tài khoản ngân hàng."
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ToDto(order);
    }

    private async Task<Order> FindOwned(
        long userId,
        long orderId,
        CancellationToken ct)
        => await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

    private static PaymentResponseDto ToDto(Order order) => new()
    {
        OrderId = order.Id, Amount = order.TotalPrice, PaymentMethod = order.PaymentMethod,
        PaymentStatus = order.PaymentStatus, OrderStatus = order.OrderStatus
    };
}
