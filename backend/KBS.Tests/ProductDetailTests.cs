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

public class ProductDetailTests
{
    [Fact]
    public async Task Detail_returns_stock_specs_sorted_images_and_hides_inactive_products()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new KbsDbContext(options);
        var product = new Product
        {
            Name = "Keyboard",
            Price = 1200000,
            Description = "Product description",
            Brand = new Brand { Name = "KBS" },
            Category = new Category { Name = "Keyboard" },
            Inventory = new Inventory { StockQuantity = 3 },
            Keycap = "PBT",
            ConnectionType = "USB",
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "/second.jpg", SortOrder = 2 },
                new() { ImageUrl = "/first.jpg", SortOrder = 1 }
            }
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new ProductService(db);
        var detail = await service.GetByIdAsync(product.Id);
        Assert.NotNull(detail);
        Assert.Equal(3, detail.StockQuantity);
        Assert.Equal("KBS", detail.BrandName);
        Assert.Equal("PBT", detail.Keycap);
        Assert.Equal("USB", detail.ConnectionType);
        Assert.Equal("Product description", detail.Description);
        Assert.Equal(new[] { "/first.jpg", "/second.jpg" }, detail.Images.Select(image => image.ImageUrl));

        var inventory = await db.Inventories.SingleAsync();
        inventory.StockQuantity = 0;
        await db.SaveChangesAsync();
        Assert.Equal(0, (await service.GetByIdAsync(product.Id))!.StockQuantity);
        db.Inventories.Remove(inventory);
        await db.SaveChangesAsync();
        Assert.Equal(0, (await service.GetByIdAsync(product.Id))!.StockQuantity);
        var savedProduct = await db.Products.SingleAsync();
        savedProduct.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Null(await service.GetByIdAsync(product.Id));
        Assert.Null(await service.GetByIdAsync(long.MaxValue));
    }

    [Fact]
    public async Task Reviews_require_completed_purchase_validate_input_and_aggregate_all_ratings()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new KbsDbContext(options);
        var product = new Product { Name = "Keyboard" };
        var user = new User { Username = "customer" };
        var order = new Order
        {
            User = user,
            OrderStatus = "SHIPPING",
            OrderDetails = new List<OrderDetail> { new() { Product = product, Quantity = 1 } }
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var service = new ProductReviewService(db);
        var review = new ProductReviewCreateDto { Rating = 5, Comment = " Good keyboard " };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(product.Id, user.Id, review));
        order.OrderStatus = "COMPLETED";
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(product.Id, user.Id,
            new ProductReviewCreateDto { Rating = 6, Comment = "Invalid" }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(product.Id, user.Id,
            new ProductReviewCreateDto { Rating = 5, Comment = " " }));
        Assert.True(await service.CreateAsync(product.Id, user.Id, review));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(product.Id, user.Id, review));
        Assert.False(await service.CreateAsync(long.MaxValue, user.Id, review));
        db.ProductReviews.Add(new ProductReview
        {
            ProductId = product.Id,
            User = new User { Username = "second" },
            Rating = 3,
            Comment = "Another review"
        });
        await db.SaveChangesAsync();
        var response = await service.GetAsync(product.Id, 5, 1);
        Assert.NotNull(response);
        Assert.Equal(2, response.TotalReviews);
        Assert.Equal(4, response.AverageRating);
        Assert.Equal(1, response.RatingCounts[3]);
        Assert.Equal("Good keyboard", Assert.Single(response.Reviews.Items).Comment);
        Assert.Equal(1, response.Reviews.TotalItems);
        Assert.Empty((await service.GetAsync(product.Id, null, 2))!.Reviews.Items);
        Assert.Null(await service.GetAsync(long.MaxValue, null, 1));
    }
}

public class ProductReviewHttpTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Review_endpoints_enforce_authentication_and_validate_payloads()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/products/9223372036854775807/reviews")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/products/1/reviews?rating=6")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/products/1/reviews?page=0")).StatusCode);
        var payload = new ProductReviewCreateDto { Rating = 5, Comment = "Good" };
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/products/1/reviews", payload)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            LoginIdentifier = "customer",
            Password = "Customer1"
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        payload.Rating = 0;
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/products/1/reviews", payload)).StatusCode);
    }
}
