using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class PaymentHttpTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Payment_routes_enforce_owner_and_staff_roles_and_do_not_expose_secrets()
    {
        using var client = factory.CreateClient();
        var options = await client.GetStringAsync("/api/payments/options");
        Assert.DoesNotContain("SecretKey", options, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AccountNumber", options, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync("/api/payments/orders/1", null)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        var adminId = await db.Users.Where(x => x.Username == "admin").Select(x => x.Id).SingleAsync();
        var owned = new Order { UserId = customerId, PaymentMethod = "BANKING", TotalPrice = 10000 };
        var other = new Order { UserId = adminId, PaymentMethod = "BANKING", TotalPrice = 10000 };
        db.Orders.AddRange(owned, other);
        await db.SaveChangesAsync();
        await Login(client, "customer", "Customer1");
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"/api/payments/orders/{owned.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsync($"/api/payments/orders/{other.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsync($"/api/payments/orders/{owned.Id}/bank-confirmation", null)).StatusCode);
        await Login(client, "staff", "Staff123");
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsync($"/api/payments/orders/{owned.Id}/bank-confirmation", null)).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal("PAID", (await db.Orders.SingleAsync(x => x.Id == owned.Id)).PaymentStatus);
        Assert.Single(await db.OrderNotes.Where(x => x.OrderId == owned.Id).ToListAsync());
    }

    private static async Task Login(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = username, Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }
}
