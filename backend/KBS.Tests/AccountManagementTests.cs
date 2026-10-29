using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Users;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class AccountManagementTests
{
    [Fact]
    public async Task Admin_crud_validates_duplicates_protects_self_and_preserves_order_history()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/roles")).StatusCode);
        foreach (var (name, password) in new[] { ("customer", "Customer1"), ("staff", "Staff123") })
        {
            await Login(client, name, password);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users/roles")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/users", NewUser())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/users/1", NewUser())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/users/1")).StatusCode);
        }
        var admin = await Login(client, "admin", "Admin123");
        var roles = await client.GetFromJsonAsync<List<UserRoleDto>>("/api/users/roles");
        var dto = NewUser();
        dto.RoleId = roles!.Single(x => x.Name == "Customer").Id;
        var created = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<UserResponseDto>())!;
        Assert.DoesNotContain("password", await created.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/users", dto)).StatusCode);
        dto.FullName = "Updated account";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/users/{user.Id}", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/status", new { })).StatusCode);
        var invalid = NewUser();
        invalid.Password = "short";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/users", invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(
            $"/api/users/{admin.Id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(
            $"/api/users/{admin.Id}/role", new { roleId = dto.RoleId })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/users/{admin.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/users/{user.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/users/{user.Id}")).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customer = await db.Users.SingleAsync(x => x.Username == "customer");
        db.Orders.Add(new Order { UserId = customer.Id });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/users/{customer.Id}")).StatusCode);
        Assert.True(await db.Orders.AnyAsync(x => x.UserId == customer.Id));
    }

    [Fact]
    public async Task Last_active_admin_cannot_be_removed_or_demoted()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var admin = await db.Users.SingleAsync(x => x.Role!.Name == "Admin");
        var customer = await db.Users.SingleAsync(x => x.Username == "customer");
        var service = new KBS.BLL.Services.UserService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStatusAsync(admin.Id, false, customer.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateRoleAsync(admin.Id, customer.RoleId, customer.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(admin.Id, customer.Id));
        Assert.True((await db.Users.AsNoTracking().SingleAsync(x => x.Id == admin.Id)).IsActive);
    }

    // MOCK DATA — isolated API test input, never used by the application UI.
    private static CreateUserDto NewUser() => new()
    {
        Username = "account-test", Email = "account-test@example.com", RoleId = 2,
        Password = "Account123", ConfirmPassword = "Account123"
    };

    private static async Task<UserProfileDto> Login(
        HttpClient client,
        string name,
        string password)
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
