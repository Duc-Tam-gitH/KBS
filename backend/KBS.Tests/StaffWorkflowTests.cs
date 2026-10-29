using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Staff;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class StaffWorkflowTests
{
    [Fact]
    public async Task Admin_inventory_reports_open_orders_and_uses_validated_stock_movements()
    {
        using var app = new TestApiFactory();
        using var admin = await Login(app, "admin", "Admin123");
        using var customer = await Login(app, "customer", "Customer1");
        using var anonymous = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        // MOCK DATA — isolated test records, never used by the application UI.
        var product = new Product
        {
            Name = "Inventory API test", CategoryId = 1,
            Brand = new Brand { Name = "Inventory test brand" },
            Inventory = new Inventory { StockQuantity = 10, LowStockThreshold = 3 }
        };
        db.Products.Add(product);
        foreach (var status in new[] { "PENDING", "CONFIRMED", "SHIPPING", "COMPLETED", "CANCELLED" })
        {
            db.Orders.Add(new Order
            {
                UserId = customerId, OrderStatus = status,
                OrderDetails = [new OrderDetail { Product = product, Quantity = 2 }]
            });
        }
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/staff/inventory")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/staff/inventory")).StatusCode);
        var items = await admin.GetFromJsonAsync<List<StaffInventoryDto>>("/api/staff/inventory");
        var item = Assert.Single(items!);
        Assert.Equal(6, item.OrderedQuantity);
        Assert.Equal(10, item.StockQuantity);
        Assert.Equal(1, item.CategoryId);
        Assert.Equal((await db.Categories.FindAsync(1L))!.Name, item.CategoryName);
        var path = $"/api/staff/inventory/{product.Id}/adjustments";
        var request = new StockAdjustmentRequest { Delta = -11, ExpectedQuantity = 10, Reason = "Count check" };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(path, request)).StatusCode);
        request.Delta = 3;
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync(path, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(path, request)).StatusCode);
        request.ExpectedQuantity = 13;
        request.Delta = -4;
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync(path, request)).StatusCode);
        var history = await admin.GetFromJsonAsync<List<InventoryMovementDto>>(
            $"/api/staff/inventory/{product.Id}/movements");
        Assert.Equal(2, history!.Count);
        Assert.Equal(13, history[0].QuantityBefore);
        Assert.Equal(9, history[0].QuantityAfter);
        Assert.Equal(await db.Users.Where(x => x.Username == "admin").Select(x => x.Id).SingleAsync(), history[0].ActorId);
    }

    [Fact]
    public async Task Tracking_generation_is_authorized_persisted_and_does_not_overwrite_existing_codes()
    {
        using var app = new TestApiFactory();
        using var anonymous = app.CreateClient();
        using var staff = await Login(app, "staff", "Staff123");
        using var customer = await Login(app, "customer", "Customer1");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        var order = new Order { UserId = customerId };
        var existing = new Order { UserId = customerId, TrackingNumber = "EXISTING-CODE" };
        db.Orders.AddRange(order, existing);
        await db.SaveChangesAsync();
        var path = $"/api/staff/orders/{order.Id}/tracking";

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsync(path, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PutAsync(path, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await staff.PutAsync($"/api/staff/orders/{long.MaxValue}/tracking", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsync(path, null)).StatusCode);
        var operations = await staff.GetFromJsonAsync<OrderOperationsDto>(
            $"/api/staff/orders/{order.Id}/operations");
        var code = operations!.TrackingNumber;
        Assert.Matches($"^TEMP-{order.Id}-[a-f0-9]{{32}}$", code!);

        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsync(path, null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await staff.PutAsync($"/api/staff/orders/{existing.Id}/tracking", null)).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(code, (await db.Orders.SingleAsync(x => x.Id == order.Id)).TrackingNumber);
        Assert.Equal("EXISTING-CODE", (await db.Orders.SingleAsync(x => x.Id == existing.Id)).TrackingNumber);
    }

    [Fact]
    public async Task Stock_adjustments_are_validated_and_logged_and_notes_are_private()
    {
        using var app = new TestApiFactory();
        using var staff = await Login(app, "staff", "Staff123");
        using var customer = await Login(app, "customer", "Customer1");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        var staffId = await db.Users.Where(x => x.Username == "staff").Select(x => x.Id).SingleAsync();
        var product = new Product
        {
            Name = Guid.NewGuid().ToString(), CategoryId = 1,
            Brand = new Brand { Name = Guid.NewGuid().ToString() },
            Inventory = new Inventory { StockQuantity = 5 }
        };
        var order = new Order { UserId = customerId };
        db.Products.Add(product);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var path = $"/api/staff/inventory/{product.Id}/adjustments";
        var dto = new StockAdjustmentRequest { Delta = -6, ExpectedQuantity = 5, Reason = "Inventory check" };
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync(path, dto)).StatusCode);
        dto.Delta = 2;
        dto.ExpectedQuantity = 4;
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync(path, dto)).StatusCode);
        dto.ExpectedQuantity = 5;
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync(path, dto)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PostAsJsonAsync(path, dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync(path, dto)).StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(7, (await db.Inventories.SingleAsync()).StockQuantity);
        var log = Assert.Single(await db.InventoryMovements.ToListAsync());
        Assert.Equal(staffId, log.ActorId);
        Assert.Equal(5, log.QuantityBefore);
        Assert.Equal(7, log.QuantityAfter);
        Assert.Equal(2, log.Delta);
        (await staff.PutAsJsonAsync($"/api/staff/inventory/{product.Id}/threshold",
            new { lowStockThreshold = 8 })).EnsureSuccessStatusCode();
        var staffSummary = await staff.GetFromJsonAsync<JsonElement>("/api/staff/summary");
        Assert.Equal(1, staffSummary.GetProperty("lowStockCount").GetInt32());
        using var admin = await Login(app, "admin", "Admin123");
        var adminSummary = await admin.GetFromJsonAsync<JsonElement>("/api/admin/dashboard");
        Assert.Equal(1, adminSummary.GetProperty("lowStockProducts").GetInt32());
        var notePath = $"/api/staff/orders/{order.Id}/notes";
        Assert.Equal(HttpStatusCode.NoContent,
            (await staff.PostAsJsonAsync(notePath, new TextRequest { Content = "Internal-only" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await customer.GetAsync($"/api/staff/orders/{order.Id}/operations")).StatusCode);
        Assert.DoesNotContain("Internal-only", await customer.GetStringAsync("/api/orders/mine"));
        Assert.Contains("Internal-only", await staff.GetStringAsync($"/api/staff/orders/{order.Id}/operations"));
    }

    [Fact]
    public async Task Customer_lock_blocks_existing_token_but_cannot_target_staff()
    {
        using var app = new TestApiFactory();
        using var staff = await Login(app, "staff", "Staff123");
        using var customer = await Login(app, "customer", "Customer1");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        var staffId = await db.Users.Where(x => x.Username == "staff").Select(x => x.Id).SingleAsync();
        Assert.Equal(HttpStatusCode.NotFound,
            (await staff.PutAsJsonAsync(
                $"/api/staff/customers/{staffId}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await staff.PutAsJsonAsync(
                $"/api/staff/customers/{customerId}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/orders/mine")).StatusCode);
        Assert.True(await db.Users.AsNoTracking().AnyAsync(x => x.Id == staffId && x.IsActive));
    }

    [Fact]
    public async Task Chat_enforces_participants_read_cursor_and_unread_counts_without_shifts()
    {
        using var app = new TestApiFactory();
        using var staff = await Login(app, "staff", "Staff123");
        using var customer = await Login(app, "customer", "Customer1");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        var adminId = await db.Users.Where(x => x.Username == "admin").Select(x => x.Id).SingleAsync();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await customer.PostAsync($"/api/chat/conversations?customerId={adminId}", null)).StatusCode);
        var opened = await customer.PostAsync("/api/chat/conversations", null);
        opened.EnsureSuccessStatusCode();
        var id = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        var prefix = $"/api/chat/conversations/{id}";
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync(prefix + "/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsync(prefix + "/claim", null)).StatusCode);
        var originalStaff = await db.Users.SingleAsync(x => x.Username == "staff");
        var secondStaff = new User
        {
            Username = Guid.NewGuid().ToString("N"), Email = Guid.NewGuid() + "@example.test",
            RoleId = originalStaff.RoleId, PasswordHash = originalStaff.PasswordHash
        };
        db.Users.Add(secondStaff);
        await db.SaveChangesAsync();
        using var colleague = await Login(app, secondStaff.Username, "Staff123");
        Assert.Equal(HttpStatusCode.Forbidden, (await colleague.GetAsync(prefix + "/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await colleague.PutAsync(prefix + "/claim", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.PostAsync("/api/staff/shift/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.PostAsync("/api/staff/shift/end", null)).StatusCode);
        Assert.Empty(await db.StaffShifts.ToListAsync());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await customer.PostAsJsonAsync(prefix + "/messages", new { content = " " })).StatusCode);
        (await customer.PostAsJsonAsync(prefix + "/messages", new { content = "First" })).EnsureSuccessStatusCode();
        var firstId = await db.ChatMessages.MaxAsync(x => x.Id);
        (await customer.PostAsJsonAsync(prefix + "/messages", new { content = "Second" })).EnsureSuccessStatusCode();
        (await staff.PutAsJsonAsync(prefix + "/read", new { throughMessageId = firstId })).EnsureSuccessStatusCode();
        Assert.Equal(1, await db.ChatMessages.CountAsync(x => x.ReadAt == null));
        var summary = await staff.GetFromJsonAsync<JsonElement>("/api/staff/summary");
        Assert.Equal(1, summary.GetProperty("unreadMessagesCount").GetInt32());
        (await staff.PostAsJsonAsync(prefix + "/messages", new { content = "Reply" })).EnsureSuccessStatusCode();
        var messages = await customer.GetFromJsonAsync<JsonElement>(prefix + "/messages");
        Assert.Equal(3, messages.GetArrayLength());
        Assert.Equal(customerId, messages[0].GetProperty("senderId").GetInt64());
        var newer = await customer.GetFromJsonAsync<JsonElement>(prefix + $"/messages?afterId={firstId}");
        Assert.Equal(2, newer.GetArrayLength());
        Assert.True(newer[0].GetProperty("id").GetInt64() > firstId);
        Assert.Empty(await db.StaffShifts.ToListAsync());
        // A different customer must not gain access by knowing the conversation id.
        using var other = app.CreateClient();
        var unique = Guid.NewGuid().ToString("N");
        var register = await other.PostAsJsonAsync("/api/auth/register", new
        {
            username = unique, email = unique + "@example.test", password = "Testing123!",
            confirmPassword = "Testing123!"
        });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(prefix + "/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PostAsJsonAsync(prefix + "/messages", new { content = "Denied" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PutAsJsonAsync(prefix + "/read", new { throughMessageId = firstId })).StatusCode);
    }

    private static async Task<HttpClient> Login(
        TestApiFactory app,
        string username,
        string password)
    {
        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = username, Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }
}
