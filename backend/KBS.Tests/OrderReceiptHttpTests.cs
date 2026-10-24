using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Orders;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class OrderReceiptHttpTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Receipt_requires_login_and_ownership_and_preserves_payment_status()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsync("/api/orders/1/receipt", null)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = "customer",
            Password = "Customer1"
        });
        login.EnsureSuccessStatusCode();
        var session = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customer = await db.Users.SingleAsync(x => x.Username == "customer");
        var other = await db.Users.SingleAsync(x => x.Username == "admin");
        var ownOrder = new Order { UserId = customer.Id, OrderStatus = "SHIPPING", PaymentStatus = "PENDING" };
        var otherOrder = new Order { UserId = other.Id, OrderStatus = "SHIPPING" };
        var pending = new Order { UserId = customer.Id, OrderStatus = "PENDING" };
        db.Orders.AddRange(ownOrder, otherOrder, pending);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync("/api/orders/0/receipt", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsync($"/api/orders/{otherOrder.Id}/receipt", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PutAsync($"/api/orders/{pending.Id}/receipt", null)).StatusCode);
        var response = await client.PutAsync($"/api/orders/{ownOrder.Id}/receipt", null);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.Equal("COMPLETED", updated!.OrderStatus);
        Assert.Equal("PENDING", updated.PaymentStatus);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsync($"/api/orders/{ownOrder.Id}/receipt", null)).StatusCode);
    }
}
