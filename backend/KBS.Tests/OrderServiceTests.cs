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
        var movement = Assert.Single(await db.InventoryMovements.ToListAsync());
        Assert.Equal(order.Id, movement.OrderId);
        Assert.Equal(-2, movement.Delta);
        Assert.Equal(3, movement.QuantityBefore);
        Assert.Equal(1, movement.QuantityAfter);
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

    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    public async Task Checkout_consumes_only_purchased_quantity_from_current_users_cart(
        int cartQuantity,
        int remainingQuantity)
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, price: 100, stock: 5);
        db.CartItems.AddRange(
            new CartItem { UserId = 1, ProductId = product.Id, Quantity = cartQuantity },
            new CartItem { UserId = 2, ProductId = product.Id, Quantity = 1 });
        await db.SaveChangesAsync();

        await new OrderService(db).CreateAsync(1, Order(product.Id, 2));

        var remaining = await db.CartItems.SingleOrDefaultAsync(x => x.UserId == 1);
        Assert.Equal(remainingQuantity, remaining?.Quantity ?? 0);
        Assert.Equal(1, (await db.CartItems.SingleAsync(x => x.UserId == 2)).Quantity);
    }

    [Fact]
    public async Task Receipt_checks_owner_and_status_and_is_idempotent_without_changing_payment_or_stock()
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, price: 100, stock: 5);
        var service = new OrderService(db);
        var created = await service.CreateAsync(1, Order(product.Id, 2));

        Assert.Null(await service.ConfirmReceiptAsync(2, created.Id));
        Assert.Null(await service.ConfirmReceiptAsync(1, long.MaxValue));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmReceiptAsync(1, created.Id));
        await service.UpdateStatusAsync(created.Id, new UpdateOrderStatusDto { OrderStatus = "CONFIRMED" }, 1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmReceiptAsync(1, created.Id));
        await service.UpdateStatusAsync(created.Id, new UpdateOrderStatusDto { OrderStatus = "SHIPPING" }, 1);

        var completed = await service.ConfirmReceiptAsync(1, created.Id);
        Assert.Equal("COMPLETED", completed!.OrderStatus);
        Assert.Equal(created.PaymentStatus, completed.PaymentStatus);
        Assert.Equal(created.TotalPrice, completed.TotalPrice);
        Assert.Equal("COMPLETED", (await service.ConfirmReceiptAsync(1, created.Id))!.OrderStatus);
        Assert.Equal(3, (await db.Inventories.SingleAsync()).StockQuantity);

        var cancelled = await service.CreateAsync(1, Order(product.Id, 1));
        await service.UpdateStatusAsync(cancelled.Id, new UpdateOrderStatusDto { OrderStatus = "CANCELLED" }, 1);
        var returned = Assert.Single(await db.InventoryMovements
            .Where(x => x.OrderId == cancelled.Id && x.Delta > 0).ToListAsync());
        Assert.Equal(1, returned.Delta);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStatusAsync(
            cancelled.Id, new UpdateOrderStatusDto { OrderStatus = "CANCELLED" }, 1));
        Assert.Single(await db.InventoryMovements.Where(x => x.OrderId == cancelled.Id && x.Delta > 0).ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmReceiptAsync(1, cancelled.Id));
    }

    [Fact]
    public async Task History_is_private_and_keeps_purchase_price_with_current_product_metadata()
    {
        await using var db = NewDb();
        var product = await SeedProduct(db, price: 100, stock: 5);
        var service = new OrderService(db);
        var created = await service.CreateAsync(1, Order(product.Id, 2));
        product.Price = 200;
        product.IsActive = false;
        product.Images.Add(new ProductImage { ImageUrl = "/later.jpg", SortOrder = 2 });
        product.Images.Add(new ProductImage { ImageUrl = "/first.jpg", SortOrder = 1 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Empty(await service.GetForUserAsync(2));
        var order = Assert.Single(await service.GetForUserAsync(1));
        var item = Assert.Single(order.OrderDetails);
        Assert.Equal(created.Id, order.Id);
        Assert.Equal(200, order.TotalPrice);
        Assert.Equal(100, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(product.Name, item.ProductName);
        Assert.Equal("/first.jpg", item.ImageUrl);
        Assert.False(item.ProductIsActive);
    }

    private static CreateOrderDto Order(long productId, int quantity) => new()
    {
        CustomerName = "Khách demo",
        PhoneNumber = "0901000001",
        ShippingAddress = "TP HCM",
        PaymentMethod = "COD",
        OrderItems = [new() { ProductId = productId, Quantity = quantity }]
    };
    private static KbsDbContext NewDb() => new(new DbContextOptionsBuilder<KbsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private static async Task<Product> SeedProduct(KbsDbContext db, decimal price, int stock)
    {
        var category = new Category { Name = "Mechanical" }; var brand = new Brand { Name = "KBS" };
        var product = new Product { Name = "Demo keyboard", Price = price, Category = category, Brand = brand, IsActive = true, Inventory = new Inventory { StockQuantity = stock } };
        db.Products.Add(product); await db.SaveChangesAsync(); return product;
    }
}
