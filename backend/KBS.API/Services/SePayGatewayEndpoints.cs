using System.Net;
using System.Text.Json;
using KBS.BLL.Services;
using KBS.Models.DTOs.Payments;
using Microsoft.Extensions.Options;

namespace KBS.API.Services;

public static class SePayGatewayEndpoints
{
    public static void MapSePayGateway(this WebApplication app)
    {
        app.MapPost("/api/payments/sepay/ipn", async (HttpRequest request,
            SePayGatewayService service, CancellationToken ct) =>
        {
            if (!service.Authenticate(request.Headers["X-Secret-Key"].ToString()))
                return Results.Unauthorized();
            if (request.ContentLength > 32768) return Results.StatusCode(413);
            try
            {
                using var json = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct);
                if (json.RootElement.ValueKind != JsonValueKind.Object) return Results.BadRequest();
                return await service.ReceiveAsync(json.RootElement, ct)
                    ? Results.Ok(new { success = true }) : Results.BadRequest(new { success = false });
            }
            catch (JsonException) { return Results.BadRequest(); }
        }).AllowAnonymous();

        app.MapGet("/api/payments/sepay/return", (IOptions<SePayGatewayOptions> settings) =>
        {
            var link = WebUtility.HtmlEncode(settings.Value.FrontendUrl.TrimEnd('/') + "/?payment=return");
            return TypedResults.Content($"""
                <!doctype html><html lang="vi"><meta charset="utf-8"><title>KBS — Thanh toán</title>
                <h1>Quay lại KBS để kiểm tra thanh toán</h1>
                <p>Kết quả được xác minh qua IPN. Trang này không xác nhận đã nhận tiền.</p>
                <a href="{link}">Mở đơn hàng của tôi</a></html>
                """, "text/html; charset=utf-8");
        }).AllowAnonymous();
    }
}
