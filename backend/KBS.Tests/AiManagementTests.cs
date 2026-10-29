using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Ai;
using KBS.Models.DTOs.Authentication;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class AiManagementTests
{
    private const string WorkerKey = "test-only-ai-worker-key-at-least-32-characters";

    private sealed class AiFactory : TestApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Ai:WorkerKey", WorkerKey);
        }
    }

    [Fact]
    public async Task Enabling_only_configures_worker_and_customer_sees_committed_decision()
    {
        using var app = new AiFactory();
        using var client = app.CreateClient();
        long orderId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
            var order = new Order { UserId = customerId, TotalPrice = 4690000 };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        await Login(client, "admin", "Admin123");
        (await client.PutAsJsonAsync("/api/admin/ai/modules/ai_auto/enabled", new { isEnabled = true }))
            .EnsureSuccessStatusCode();
        var events = await client.GetFromJsonAsync<AiEventsPage>("/api/admin/ai/events");
        Assert.Equal("ENABLED", Assert.Single(events!.Items).Kind);
        Assert.Null(events.Items[0].OrderId);

        await Login(client, "customer", "Customer1");
        var before = await client.GetFromJsonAsync<List<KBS.Models.DTOs.Orders.OrderResponseDto>>("/api/orders/mine");
        Assert.Equal("PENDING", Assert.Single(before!).OrderStatus);

        client.DefaultRequestHeaders.Add("X-AI-Key", WorkerKey);
        (await client.PostAsJsonAsync($"/api/ai-worker/auto/orders/{orderId}/decision", new { decision = "CONFIRM" }))
            .EnsureSuccessStatusCode();
        var after = await client.GetFromJsonAsync<List<KBS.Models.DTOs.Orders.OrderResponseDto>>("/api/orders/mine");
        Assert.Equal("CONFIRMED", Assert.Single(after!).OrderStatus);

        await Login(client, "admin", "Admin123");
        events = await client.GetFromJsonAsync<AiEventsPage>("/api/admin/ai/events");
        var confirmed = Assert.Single(events!.Items, x => x.OrderId == orderId);
        Assert.Equal("CONFIRMED", confirmed.Kind);
    }

    [Theory]
    [InlineData(9999999, "CONFIRM", "CONFIRMED")]
    [InlineData(10000000, "MANUAL_REVIEW", "PENDING")]
    [InlineData(10000001, "MANUAL_REVIEW", "PENDING")]
    public async Task Decisions_use_database_threshold_and_are_idempotent(
        decimal total,
        string decision,
        string expected)
    {
        using var app = new AiFactory();
        using var client = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var service = new AiManagementService(db);
        var userId = await db.Users.Select(x => x.Id).FirstAsync();
        var order = new Order { UserId = userId, TotalPrice = total, PaymentStatus = "PAID" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await service.EnableAsync("ai_auto", true, userId, default);
        var poll = await service.PollAsync(default);
        Assert.Equal(total, Assert.Single(poll.Orders).TotalPrice);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DecideAsync(
            order.Id, decision == "CONFIRM" ? "MANUAL_REVIEW" : "CONFIRM", default));
        await service.DecideAsync(order.Id, decision, default);
        await service.DecideAsync(order.Id, decision, default);
        Assert.Equal(expected, order.OrderStatus);
        Assert.Equal("PAID", order.PaymentStatus);
        Assert.Empty(await db.InventoryMovements.ToListAsync());
        Assert.Single(await db.AiEvents.Where(x => x.OrderId == order.Id).ToListAsync());
        Assert.Empty((await service.PollAsync(default)).Orders);
    }

    [Theory]
    [InlineData("CANCELLED")]
    [InlineData("CONFIRMED")]
    [InlineData("SHIPPING")]
    [InlineData("COMPLETED")]
    public async Task Non_pending_orders_are_never_changed(string status)
    {
        using var app = new AiFactory();
        using var client = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var id = await db.Users.Select(x => x.Id).FirstAsync();
        var order = new Order { UserId = id, OrderStatus = status, TotalPrice = 100 };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var service = new AiManagementService(db);
        await service.EnableAsync("ai_auto", true, id, default);
        Assert.Equal("SKIPPED", (await service.DecideAsync(order.Id, "CONFIRM", default)).Result);
        Assert.Equal(status, order.OrderStatus);
        Assert.Empty(await db.AiEvents.Where(x => x.OrderId != null).ToListAsync());
    }

    [Fact]
    public async Task Http_enforces_roles_worker_key_toggle_and_event_filtering()
    {
        using var app = new AiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/ai/modules")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync("/api/ai-worker/auto/poll", null)).StatusCode);
        foreach (var (name, password) in new[] { ("customer", "Customer1"), ("staff", "Staff123") })
        {
            await Login(client, name, password);
            var readStatus = name == "staff" ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
            Assert.Equal(readStatus, (await client.GetAsync("/api/admin/ai/modules")).StatusCode);
            Assert.Equal(readStatus, (await client.GetAsync("/api/admin/ai/events")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.PutAsJsonAsync(
                    "/api/admin/ai/modules/ai_auto/enabled", new { isEnabled = true })).StatusCode);
        }
        await Login(client, "admin", "Admin123");
        var modules = await client.GetFromJsonAsync<List<AiModuleResponseDto>>("/api/admin/ai/modules");
        Assert.Equal(5, modules!.Count);
        Assert.All(modules, x => Assert.False(x.IsEnabled));
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PutAsJsonAsync(
                "/api/admin/ai/modules/ai_anti/enabled", new { isEnabled = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync(
                "/api/admin/ai/modules/missing/enabled", new { isEnabled = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync("/api/admin/ai/modules/ai_auto/enabled", new { })).StatusCode);
        (await client.PutAsJsonAsync("/api/admin/ai/modules/ai_auto/enabled", new { isEnabled = true }))
            .EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/admin/ai/modules/ai_auto/enabled", new { isEnabled = true }))
            .EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("X-AI-Key", WorkerKey);
        (await client.PostAsync("/api/ai-worker/auto/poll", null)).EnsureSuccessStatusCode();
        modules = await client.GetFromJsonAsync<List<AiModuleResponseDto>>("/api/admin/ai/modules");
        Assert.Equal("ONLINE", modules!.Single(x => x.Id == "ai_auto").ConnectionStatus);
        (await client.PutAsJsonAsync("/api/admin/ai/modules/ai_auto/enabled", new { isEnabled = false }))
            .EnsureSuccessStatusCode();
        var events = await client.GetFromJsonAsync<AiEventsPage>("/api/admin/ai/events?limit=1");
        Assert.Single(events!.Items);
        Assert.NotNull(events.NextBeforeId);
        var older = await client.GetFromJsonAsync<AiEventsPage>(
            $"/api/admin/ai/events?moduleId=ai_auto&beforeId={events.NextBeforeId}");
        Assert.Equal("ENABLED", Assert.Single(older!.Items).Kind);
        var empty = await client.GetFromJsonAsync<AiEventsPage>("/api/admin/ai/events?moduleId=ai_anti");
        Assert.Empty(empty!.Items);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var order = new Order { UserId = await db.Users.Select(x => x.Id).FirstAsync(), TotalPrice = 100 };
        db.Orders.Add(order);
        var module = await db.AiModules.SingleAsync(x => x.Id == "ai_auto");
        module.LastHeartbeatAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var service = new AiManagementService(db);
        Assert.Equal("SKIPPED", (await service.DecideAsync(order.Id, "CONFIRM", default)).Result);
        Assert.Equal("PENDING", order.OrderStatus);
        Assert.Equal("OFFLINE", (await service.ModulesAsync(default)).Single(x => x.Id == "ai_auto").ConnectionStatus);
        Assert.Empty((await service.PollAsync(default)).Orders);
    }

    private static async Task Login(
        HttpClient client,
        string name,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = name, Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }
}
