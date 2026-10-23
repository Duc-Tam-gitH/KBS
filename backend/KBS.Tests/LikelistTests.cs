using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.Tests;

public class LikelistTests
{
    [Fact]
    public async Task Likes_are_unique_private_and_keep_unavailable_products_visible()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new KbsDbContext(options);
        var product = new Product
        {
            Name = "Keyboard",
            Price = 2500000,
            Brand = new Brand { Name = "KBS" },
            Category = new Category { Name = "Mechanical" },
            Inventory = new Inventory { StockQuantity = 0 },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "/keyboard.jpg", SortOrder = 0 }
            }
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var service = new LikelistService(db);

        Assert.True(await service.AddAsync(1, product.Id));
        Assert.True(await service.AddAsync(1, product.Id));
        Assert.Single(await db.Likelist.ToListAsync());
        Assert.Empty(await service.GetAsync(2));
        Assert.False(await service.RemoveAsync(2, product.Id));
        Assert.False(await service.AddAsync(1, long.MaxValue));

        product.Price = 2000000;
        product.IsActive = false;
        await db.SaveChangesAsync();
        var item = Assert.Single(await service.GetAsync(1));
        Assert.Equal(2000000, item.Price);
        Assert.Equal("KBS", item.BrandName);
        Assert.Equal("/keyboard.jpg", Assert.Single(item.Images).ImageUrl);
        Assert.Equal(0, item.StockQuantity);
        Assert.False(item.IsActive);
        Assert.False(await service.AddAsync(2, product.Id));
        Assert.True(await service.RemoveAsync(1, product.Id));
        Assert.False(await service.RemoveAsync(1, product.Id));
        Assert.Empty(await service.GetAsync(1));
    }
}

public class LikelistHttpTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Endpoints_require_login_and_validate_product_ids()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/likelist")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsync("/api/likelist/items/1", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.DeleteAsync("/api/likelist/items/1")).StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = "customer",
            Password = "Customer1"
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/likelist")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsync("/api/likelist/items/0", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsync("/api/likelist/items/9223372036854775807", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.DeleteAsync("/api/likelist/items/9223372036854775807")).StatusCode);
    }
}
