using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using KBS.DAL.Data;
using KBS.Models.DTOs.Payments;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KBS.BLL.Services;

public class PaymentService(
    KbsDbContext db,
    IOptions<PaymentOptions> options)
{
    public bool BankAvailable => Regex.IsMatch(options.Value.Bank.BankBin, @"^\d{6}$")
        && Regex.IsMatch(options.Value.Bank.AccountNumber, @"^\d{6,19}$")
        && !string.IsNullOrWhiteSpace(options.Value.Bank.AccountName);

    public void ValidateMethod(string method)
    {
        if (method == "BANKING" && !BankAvailable)
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
        ValidateMethod(order.PaymentMethod);
        if (order.PaymentMethod == "BANKING")
        {
            if (order.TotalPrice <= 0 || order.TotalPrice != decimal.Truncate(order.TotalPrice))
                throw new InvalidOperationException("QR chuyển khoản yêu cầu số tiền nguyên dương.");
            var bank = options.Value.Bank;
            result.TransferContent = $"KBS{order.Id}";
            result.BankName = bank.BankName;
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
