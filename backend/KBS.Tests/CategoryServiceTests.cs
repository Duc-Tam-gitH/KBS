using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.DTOs.Categories;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task Category_lifecycle_validates_names_counts_products_and_protects_used_categories()
    {
        var options = new DbContextOptionsBuilder<KbsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new KbsDbContext(options);
        var service = new CategoryService(db);
        Assert.Empty(await service.GetAllAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new() { Name = " " }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new() { Name = null! }));
        var created = await service.CreateAsync(new() { Name = " Mechanical ", IsActive = true });
        Assert.Equal("Mechanical", created.Name);
        Assert.Equal(0, created.ProductCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new() { Name = "mechanical" }));
        var updated = await service.UpdateAsync(created.Id, new CategoryUpdateDto
        {
            Name = "Mechanical", Description = " Updated ", IsActive = false
        });
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.Equal("Updated", updated.Description);
        db.Products.Add(new Product { CategoryId = created.Id, Name = "Keyboard" });
        await db.SaveChangesAsync();
        Assert.Equal(1, (await service.GetAllAsync()).Single().ProductCount);
        Assert.Equal(1, (await service.GetByIdAsync(created.Id))!.ProductCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(created.Id));
        Assert.NotNull(await service.GetByIdAsync(created.Id));
        db.Products.RemoveRange(db.Products);
        await db.SaveChangesAsync();
        Assert.True(await service.DeleteAsync(created.Id));
        Assert.False(await service.DeleteAsync(created.Id));
        Assert.Null(await service.UpdateAsync(created.Id, new() { Name = "Missing" }));
    }
}
