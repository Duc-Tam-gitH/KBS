using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Orders;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KBS.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task Create_order_uses_server_price_and_decrements_inventory()
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, price: 2500000, stock: 3);
        var order = await new OrderService(db).CreateAsync(1, Order(product.Id, 2));
        Assert.Equal(5000000, order.TotalPrice);
        Assert.Equal(2500000, Assert.Single(order.OrderDetails).UnitPrice);
        Assert.Equal(1, (await db.Inventories.SingleAsync()).StockQuantity);
    }

    [Fact]
    public async Task Create_order_rejects_when_stock_is_insufficient_without_changing_stock()
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, price: 100, stock: 1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new OrderService(db).CreateAsync(1, Order(product.Id, 2)));
        Assert.Equal(1, (await db.Inventories.SingleAsync()).StockQuantity);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    private static CreateOrderDto Order(long productId, int quantity) => new() { CustomerName = "Khách demo", PhoneNumber = "0901000001", ShippingAddress = "TP HCM", PaymentMethod = "COD", OrderItems = [new() { ProductId = productId, Quantity = quantity }] };
    private static KbsDbContext NewDb() => new(new DbContextOptionsBuilder<KbsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private static async Task<Product> SeedProduct(KbsDbContext db, decimal price, int stock)
    {
        var category = new Category { Name = "Mechanical" }; var brand = new Brand { Name = "KBS" };
        var product = new Product { Name = "Demo keyboard", Price = price, Category = category, Brand = brand, IsActive = true, Inventory = new Inventory { StockQuantity = stock } };
        db.Products.Add(product); await db.SaveChangesAsync(); return product;
    }
}
