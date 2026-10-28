using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Cart;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.Tests;

public class CartServiceTests
{
    [Fact]
    public async Task Cart_returns_product_specs_and_first_image_or_null()
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, 100, 3);
        product.Color = "Black";
        product.Layout = "75%";
        product.Images.Add(new ProductImage { ImageUrl = "/second.jpg", SortOrder = 2 });
        product.Images.Add(new ProductImage { ImageUrl = "/first.jpg", SortOrder = 0 });
        await db.SaveChangesAsync();
        var service = new CartService(db);
        var result = await service.AddAsync(1, new AddCartItemDto { ProductId = product.Id, Quantity = 2 });
        var item = Assert.Single(result.Items);
        Assert.Equal("/first.jpg", item.ImageUrl);
        Assert.Equal("Black", item.Color);
        Assert.Equal("75%", item.Layout);
        Assert.Equal(200, item.Subtotal);
        db.ProductImages.RemoveRange(product.Images);
        await db.SaveChangesAsync();
        Assert.Null(Assert.Single((await service.GetAsync(1)).Items).ImageUrl);
        Assert.Empty((await service.GetAsync(2)).Items);
    }

    [Fact]
    public async Task Add_merges_existing_item_and_uses_current_product_price()
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, price: 2500000, stock: 3);
        var service = new CartService(db);

        await service.AddAsync(1, new AddCartItemDto
        {
            ProductId = product.Id,
            Quantity = 1
        });
        var cart = await service.AddAsync(1, new AddCartItemDto
        {
            ProductId = product.Id,
            Quantity = 2
        });

        var item = Assert.Single(cart.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(7500000, cart.TotalPrice);
    }

    private static KbsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new KbsDbContext(options);
    }

    private static async Task<Product> SeedProduct(
        KbsDbContext db,
        decimal price,
        int stock)
    {
        var product = new Product
        {
            Name = "Demo keyboard",
            Price = price,
            Category = new Category { Name = "Mechanical" },
            Brand = new Brand { Name = "KBS" },
            IsActive = true,
            Inventory = new Inventory { StockQuantity = stock }
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}
