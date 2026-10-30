using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Shop;
using KBS.Models.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class ShopSettingsTests
{
    [Fact]
    public async Task Home_content_and_availability_preserve_admin_access()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        Assert.True((await client.GetFromJsonAsync<PublicShopDto>("/api/shop"))!.IsOpen);
        await Login(client, "staff", "Staff123");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("/api/admin/shop/availability", new { isOpen = false })).StatusCode);
        await Login(client, "admin", "Admin123");
        var settings = Settings();
        settings.HomeContent = new() { ["hero.text2"] = "Updated title" };
        (await client.PutAsJsonAsync("/api/admin/shop", settings)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/admin/shop/availability", new { isOpen = false })).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/admin/shop", settings)).EnsureSuccessStatusCode();
        var shop = await client.GetFromJsonAsync<PublicShopDto>("/api/shop");
        Assert.False(shop!.IsOpen);
        Assert.Equal("Updated title", shop.HomeContent["hero.text2"]);
        settings.HomeContent["hero.image1.src"] = "javascript:alert(1)";
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync("/api/admin/shop", settings)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/products")).StatusCode);
        await Login(client, "customer", "Customer1");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/orders", new { })).StatusCode);
        await Login(client, "admin", "Admin123");
        (await client.GetAsync("/api/admin/shop")).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/admin/shop/availability", new { isOpen = true })).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetAsync("/api/products")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Settings_are_admin_only_public_fields_are_safe_and_payments_use_saved_bank()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        var empty = await client.GetFromJsonAsync<PublicShopDto>("/api/shop");
        Assert.Null(empty!.Name);
        Assert.Null(empty.UpdatedAt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/shop")).StatusCode);
        foreach (var (name, password) in new[] { ("staff", "Staff123"), ("customer", "Customer1") })
        {
            await Login(client, name, password);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/shop")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/admin/shop", Settings())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/admin/shop/media/logo")).StatusCode);
        }
        await Login(client, "admin", "Admin123");
        var invalid = Settings(); invalid.BankBin = "bad";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/admin/shop", invalid)).StatusCode);
        invalid = Settings(); invalid.AccountName = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/admin/shop", invalid)).StatusCode);
        invalid = Settings(); invalid.Name = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/admin/shop", invalid)).StatusCode);
        (await client.PutAsJsonAsync("/api/admin/shop", Settings())).EnsureSuccessStatusCode();
        var saved = await client.GetFromJsonAsync<AdminShopDto>("/api/admin/shop");
        Assert.Equal("Test shop", saved!.Shop.Name);
        Assert.NotNull(saved.Shop.UpdatedAt);
        Assert.Equal("1234567890", saved.AccountNumber);
        var publicJson = await client.GetStringAsync("/api/shop");
        Assert.DoesNotContain("accountNumber", publicJson);
        Assert.DoesNotContain("bankBin", publicJson);
        Assert.DoesNotContain("1234567890", publicJson);
        var customer = await Login(client, "customer", "Customer1");
        long orderId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            var order = new Order { UserId = customer.Id, TotalPrice = 125000, PaymentMethod = "BANKING" };
            db.Orders.Add(order); await db.SaveChangesAsync(); orderId = order.Id;
        }
        var payment = await client.PostAsync($"/api/payments/orders/{orderId}", null);
        payment.EnsureSuccessStatusCode();
        Assert.Contains("970422-1234567890", await payment.Content.ReadAsStringAsync());
        await Login(client, "admin", "Admin123");
        var cleared = new SaveShopSettingsDto { Name = "Test shop" };
        (await client.PutAsJsonAsync("/api/admin/shop", cleared)).EnsureSuccessStatusCode();
        Assert.Contains("false", await client.GetStringAsync("/api/payments/options"));
    }

    [Fact]
    public async Task Images_validate_type_size_and_can_be_read_and_removed()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        await Login(client, "admin", "Admin123");
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/l9sAAAAASUVORK5CYII=");
        using var upload = Image(png, "image/png");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/admin/shop/media/logo", upload)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        var image = await client.GetAsync("/api/shop/media/logo");
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal(png, await image.Content.ReadAsByteArrayAsync());
        await Login(client, "admin", "Admin123");
        using var fake = Image("<script>alert(1)</script>"u8.ToArray(), "image/png");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync("/api/admin/shop/media/logo", fake)).StatusCode);
        using var large = Image(new byte[2 * 1024 * 1024 + 1], "image/png");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync("/api/admin/shop/media/logo", large)).StatusCode);
        (await client.DeleteAsync("/api/admin/shop/media/logo")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/shop/media/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/shop/media/other")).StatusCode);
    }

    private static MultipartFormDataContent Image(byte[] bytes, string type)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(type);
        form.Add(content, "file", "test.png");
        return form;
    }

    // Isolated test data only; no seed data is added to the application database.
    private static SaveShopSettingsDto Settings() => new()
    {
        Name = "Test shop", Slogan = "Test slogan", Description = "Test description",
        Email = "shop@example.com", WarehouseAddress = "Test address",
        BankName = "Test bank", BankBin = "970422", AccountNumber = "1234567890", AccountName = "TEST ACCOUNT"
    };

    private static async Task<UserProfileDto> Login(HttpClient client, string name, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = name, Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth.User;
    }
}
