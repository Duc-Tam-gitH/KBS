using System.Net;
using System.Net.Http.Json;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Ai;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KBS.Tests;

public class RecommendationTests
{
    private const string Key = "recommender-test-key-at-least-32-characters";
    private sealed class Factory : TestApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Ai:WorkerKey", Key);
        }
    }

    [Fact]
    public async Task Personalized_results_use_completed_history_and_authenticated_identity()
    {
        using var app = new Factory();
        using var client = app.CreateClient();
        Assert.Equal("DISABLED", (await client.GetFromJsonAsync<RecommendationPage>("/api/recommendations"))!.Status);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/ai-worker/recommender/poll", new { })).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var customerId = await db.Users.Where(x => x.Username == "customer").Select(x => x.Id).SingleAsync();
        var module = await db.AiModules.SingleAsync(x => x.Id == "ai_recommender");
        module.IsEnabled = true;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        var category = new Category { Name = "Recommendation category" };
        var brand = new Brand { Name = "Recommendation brand", IsActive = true };
        var products = Enumerable.Range(1, 9).Select(i => new Product
        {
            Name = $"Test {i}", Category = category, Brand = brand, Price = 100,
            Inventory = new Inventory { StockQuantity = i == 9 ? 0 : 5 }
        }).ToArray();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
        for (var i = 0; i < 6; i++)
        {
            db.Orders.Add(new Order
            {
                UserId = customerId, OrderStatus = "COMPLETED", CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
                OrderDetails = [new OrderDetail { ProductId = products[i].Id, Quantity = 1, UnitPrice = 100 }]
            });
        }
        db.Orders.Add(new Order
        {
            UserId = customerId, OrderStatus = "CANCELLED",
            OrderDetails = [new OrderDetail { ProductId = products[6].Id, Quantity = 1 }]
        });
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-AI-Key", Key);
        var poll = await (await client.PostAsJsonAsync("/api/ai-worker/recommender/poll", new { }))
            .Content.ReadFromJsonAsync<RecommendationPoll>();
        var history = Assert.Single(poll!.Histories);
        Assert.Equal(5, history.Products.Count);
        Assert.Equal(products[0].Id, history.Products[0].Id);
        Assert.DoesNotContain(history.Products, p => p.Id == products[5].Id || p.Id == products[6].Id);
        Assert.DoesNotContain(poll.Products, p => p.Id == products[8].Id);
        var update = new RecommendationUpdate
        {
            Version = poll.Version, Random = [products[7].Id],
            Customers = [new CustomerRecommendations { UserId = customerId, ProductIds = [products[6].Id] }]
        };
        (await client.PostAsJsonAsync("/api/ai-worker/recommender/results", update)).EnsureSuccessStatusCode();
        var guest = await client.GetFromJsonAsync<RecommendationPage>($"/api/recommendations?userId={customerId}");
        Assert.Equal("RANDOM", guest!.Mode);
        Assert.Equal(products[7].Id, Assert.Single(guest.Items).Id);
        var noHistoryUser = await db.Users.Where(x => x.Username == "staff").Select(x => x.Id).SingleAsync();
        var service = scope.ServiceProvider.GetRequiredService<RecommendationService>();
        Assert.Equal("RANDOM", (await service.GetAsync(noHistoryUser, default)).Mode);
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { loginIdentifier = "customer", password = "Customer1" });
        login.EnsureSuccessStatusCode();
        var session = await login.Content.ReadFromJsonAsync<KBS.Models.DTOs.Authentication.AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", session!.AccessToken);
        var personal = await client.GetFromJsonAsync<RecommendationPage>("/api/recommendations?userId=999");
        Assert.Equal("PERSONALIZED", personal!.Mode);
        Assert.Equal(products[6].Id, Assert.Single(personal.Items).Id);
        update.Customers[0].ProductIds = [products[0].Id];
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/ai-worker/recommender/results", update)).StatusCode);
        products[6].Inventory!.StockQuantity = 0;
        await db.SaveChangesAsync();
        Assert.Empty((await client.GetFromJsonAsync<RecommendationPage>("/api/recommendations"))!.Items);
        module.IsEnabled = false;
        await db.SaveChangesAsync();
        Assert.Equal("DISABLED", (await client.GetFromJsonAsync<RecommendationPage>("/api/recommendations"))!.Status);
        module.IsEnabled = true;
        module.UpdatedAt = module.UpdatedAt.AddSeconds(1);
        await db.SaveChangesAsync();
        Assert.Equal("WAITING", (await client.GetFromJsonAsync<RecommendationPage>("/api/recommendations"))!.Status);
    }
    [Fact]
    public async Task Product_sound_round_trips_through_create_and_update()
    {
        using var app = new Factory();
        using var client = app.CreateClient();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var category = new Category { Name = "Test category" };
        var brand = new Brand { Name = "Test brand", IsActive = true };
        db.AddRange(category, brand);
        await db.SaveChangesAsync();
        var service = new ProductService(db);
        var product = await service.CreateAsync(new ProductCreateDto
        {
            Name = "Test keyboard", Price = 100, CategoryId = category.Id, BrandId = brand.Id, Sound = " Thocky "
        });
        Assert.Equal("Thocky", product.Sound);
        var updated = await service.UpdateAsync(product.Id, new ProductUpdateDto
        {
            Name = product.Name, Price = product.Price, CategoryId = category.Id, BrandId = brand.Id,
            Sound = "Clacky", IsActive = true
        });
        Assert.Equal("Clacky", updated!.Sound);
    }
}
