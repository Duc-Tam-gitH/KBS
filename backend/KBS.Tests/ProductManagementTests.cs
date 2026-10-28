using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Authentication;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.Tests;

public class ProductManagementTests
{
    [Fact]
    public async Task Management_includes_inactive_products_without_exposing_them_in_catalog()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new KbsDbContext(options);
        var hidden = new Product
        {
            Name = "Hidden", IsActive = false,
            Category = new Category { Name = "Category" },
            Brand = new Brand { Name = "Brand" },
            Inventory = new Inventory { StockQuantity = 7 },
            Images = new List<ProductImage> { new() { ImageUrl = "/image.jpg" } }
        };
        db.Products.AddRange(hidden, new Product
        {
            Name = "Active", Category = hidden.Category, Brand = hidden.Brand,
            Inventory = new Inventory()
        });
        await db.SaveChangesAsync();
        var service = new ProductService(db);
        var managed = await service.GetForManagementAsync();
        Assert.Equal(2, managed.Count);
        var result = Assert.Single(managed, item => !item.IsActive);
        Assert.Equal(7, result.StockQuantity);
        Assert.Equal("Category", result.CategoryName);
        Assert.Equal("Brand", result.BrandName);
        Assert.Equal("/image.jpg", Assert.Single(result.Images).ImageUrl);
        var catalog = await service.GetAllAsync(null, null, null, null, null, 1, 10);
        Assert.True(Assert.Single(catalog.Items).IsActive);
        Assert.Null(await service.GetByIdAsync(hidden.Id));
    }
}

public class ProductManagementHttpTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Management_requires_admin_role()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/products/management")).StatusCode);
        foreach (var (username, password, expected) in new[]
        {
            ("customer", "Customer1", HttpStatusCode.Forbidden),
            ("staff", "Staff123", HttpStatusCode.Forbidden),
            ("admin", "Admin123", HttpStatusCode.OK)
        })
        {
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                LoginIdentifier = username, Password = password
            });
            login.EnsureSuccessStatusCode();
            var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
            Assert.Equal(expected, (await client.GetAsync("/api/products/management")).StatusCode);
        }
    }

    [Fact]
    public async Task Admin_can_create_edit_and_stop_selling_without_losing_images_or_specs()
    {
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = "admin", Password = "Admin123"
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var brandResponse = await client.PostAsJsonAsync("/api/brands", new { Name = Guid.NewGuid().ToString() });
        brandResponse.EnsureSuccessStatusCode();
        var brand = await brandResponse.Content.ReadFromJsonAsync<KBS.Models.DTOs.Brands.BrandResponseDto>();
        var createdResponse = await client.PostAsJsonAsync("/api/products", new ProductCreateDto
        {
            Name = "Keyboard", CategoryId = 1, BrandId = brand!.Id, Price = 100,
            Keycap = "PBT", IsRGB = true,
            Images = new List<ProductImageDto> { new() { ImageUrl = "https://example.com/keyboard.jpg", SortOrder = 0 } }
        });
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal(0, created!.StockQuantity);
        var savedResponse = await client.PutAsJsonAsync($"/api/products/{created.Id}", new ProductUpdateDto
        {
            Name = created.Name, Price = 200, CategoryId = created.CategoryId, BrandId = created.BrandId,
            Keycap = created.Keycap, IsRGB = created.IsRGB, Images = created.Images, IsActive = false
        });
        savedResponse.EnsureSuccessStatusCode();
        var saved = await savedResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.False(saved!.IsActive);
        Assert.Equal(200, saved.Price);
        Assert.Equal("PBT", saved.Keycap);
        Assert.True(saved.IsRGB);
        Assert.Equal("https://example.com/keyboard.jpg", Assert.Single(saved.Images).ImageUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/products/{created.Id}")).StatusCode);
        var managed = await client.GetFromJsonAsync<List<ProductResponseDto>>("/api/products/management");
        Assert.Contains(managed!, item => item.Id == created.Id && !item.IsActive);
    }
}
