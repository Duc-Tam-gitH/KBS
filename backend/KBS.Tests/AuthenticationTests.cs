using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.API.Data;
using KBS.BLL.Interfaces;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KBS.Tests;

public class AuthenticationTests
{
    [Fact]
    public async Task Register_rejects_duplicate_email_and_phone_and_hashes_password()
    {
        await using var db = NewDb();
        await SeedRoles(db);
        var service = new AuthService(db, Jwt());
        var result = await service.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Email = "Alice@Example.com",
            PhoneNumber = "0900000001",
            Password = "Password1",
            ConfirmPassword = "Password1"
        });
        Assert.Equal("alice@example.com", result.User.Email);
        Assert.NotEqual("Password1", (await db.Users.SingleAsync()).PasswordHash);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterDto
            {
                Username = "bob",
                Email = "ALICE@example.com",
                Password = "Password1",
                ConfirmPassword = "Password1"
            }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterDto
            {
                Username = "bob",
                Email = "bob@example.com",
                PhoneNumber = "0900000001",
                Password = "Password1",
                ConfirmPassword = "Password1"
            }));
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("abcdefgh")]
    [InlineData("12345678")]
    public async Task Register_enforces_password_policy(string password)
    {
        await using var db = NewDb();
        await SeedRoles(db);
        var service = new AuthService(db, Jwt());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterDto
            {
                Username = "alice",
                Email = "alice@example.com",
                Password = password,
                ConfirmPassword = password
            }));
    }

    [Fact]
    public async Task Login_rejects_inactive_user_and_generates_valid_jwt_for_active_user()
    {
        await using var db = NewDb();
        await SeedRoles(db);
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Username = "alice",
            Email = "alice@example.com",
            RoleId = 2,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, "Password1");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new AuthService(db, Jwt());
        var response = await service.LoginAsync(
            new LoginDto { LoginIdentifier = "alice", Password = "Password1" });
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Contains(token.Claims, c => c.Type.EndsWith("role") && c.Value == "Customer");
        user.IsActive = false;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginDto { LoginIdentifier = "alice", Password = "Password1" }));
    }

    [Fact]
    public async Task Admin_seeder_creates_once_and_requires_strong_password()
    {
        await using var db = NewDb(); await SeedRoles(db);
        var name = Guid.NewGuid().ToString();
        var provider = new ServiceCollection()
            .AddDbContext<KbsDbContext>(o => o.UseInMemoryDatabase(name))
            .BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var seedContext = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            await SeedRoles(seedContext);
        }
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminBootstrap:Username"] = "admin",
                ["AdminBootstrap:Email"] = "admin@example.com",
                ["AdminBootstrap:Password"] = "Admin123"
            })
            .Build();
        await AdminSeeder.SeedAsync(provider, config);
        await AdminSeeder.SeedAsync(provider, config);
        using var verifyScope = provider.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var admin = Assert.Single(await context.Users.ToListAsync());
        Assert.NotEqual("Admin123", admin.PasswordHash);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            new PasswordHasher<User>().VerifyHashedPassword(admin, admin.PasswordHash, "Admin123"));

        var weakConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminBootstrap:Username"] = "other",
                ["AdminBootstrap:Email"] = "other@example.com",
                ["AdminBootstrap:Password"] = "weak"
            })
            .Build();
        var weakProvider = new ServiceCollection()
            .AddDbContext<KbsDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .BuildServiceProvider();
        using (var scope = weakProvider.CreateScope())
        {
            await SeedRoles(scope.ServiceProvider.GetRequiredService<KbsDbContext>());
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AdminSeeder.SeedAsync(weakProvider, weakConfig));
    }

    private static KbsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new KbsDbContext(options);
    }
    private static async Task SeedRoles(KbsDbContext db)
    {
        db.Roles.AddRange(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Customer" },
            new Role { Id = 3, Name = "Staff" });
        await db.SaveChangesAsync();
    }
    private static IJwtService Jwt()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:SecretKey"] = "test-secret-key-that-is-long-enough-for-hmac"
            })
            .Build();
        return new JwtService(configuration);
    }
}

public class AuthorizationHttpTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;
    public AuthorizationHttpTests(TestApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Admin_endpoint_returns_401_without_token_403_for_customer_and_200_for_admin()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/users")).StatusCode);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await Login("customer", "Customer1"));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/users")).StatusCode);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await Login("admin", "Admin123"));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task Staff_can_access_operational_endpoints_but_not_administration_endpoints()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await Login("staff", "Staff123"));

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/inventory/product/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/admin/dashboard")).StatusCode);
    }

    private async Task<string> Login(string id, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { LoginIdentifier = id, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }
}

public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        builder.UseSetting("Jwt:Issuer", "test");
        builder.UseSetting("Jwt:Audience", "test");
        builder.UseSetting("Jwt:SecretKey", "test-secret-key-that-is-long-enough-for-hmac");
        builder.UseSetting("ConnectionStrings:KBSConnection", "Host=unused");
        builder.UseSetting("AdminBootstrap:Username", "admin");
        builder.UseSetting("AdminBootstrap:Email", "admin@example.com");
        builder.UseSetting("AdminBootstrap:Password", "Admin123");
        builder.ConfigureServices(services =>
        {
            var databaseRegistrations = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<KbsDbContext>) ||
                d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure
                    .IDbContextOptionsConfiguration<KbsDbContext>)).ToList();
            foreach (var registration in databaseRegistrations)
            {
                services.Remove(registration);
            }
            services.AddDbContext<KbsDbContext>(o => o.UseInMemoryDatabase(_databaseName));
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            db.Database.EnsureCreated();
            if (!db.Roles.Any(r => r.Name == "Staff"))
            {
                db.Roles.Add(new Role { Id = 3, Name = "Staff", Description = "Nhân viên vận hành" });
                db.SaveChanges();
            }
            if (!db.Users.Any(u => u.Username == "customer"))
            {
                var user = new User
                {
                    Username = "customer",
                    Email = "customer@example.com",
                    RoleId = 2,
                    IsActive = true
                };
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Customer1");
                db.Users.Add(user);
                db.SaveChanges();
            }
            if (!db.Users.Any(u => u.Username == "staff"))
            {
                var user = new User
                {
                    Username = "staff",
                    Email = "staff@example.com",
                    RoleId = 3,
                    IsActive = true
                };
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Staff123");
                db.Users.Add(user);
                db.SaveChanges();
            }
        });
    }
}
