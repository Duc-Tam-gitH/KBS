
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.DAL.Data;

public class KbsDbContext(DbContextOptions<KbsDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category 1 - N Product
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Brand 1 - N Product
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Product 1 - 1 Inventory
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Inventory)
            .WithOne(i => i.Product)
            .HasForeignKey<Inventory>(i => i.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Role 1 - N User
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // User 1 - N Order
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Order 1 - N OrderDetail
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Product 1 - N OrderDetail
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Product)
            .WithMany(p => p.OrderDetails)
            .HasForeignKey(od => od.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // Decimal precision for money
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.OriginalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.UnitPrice)
            .HasPrecision(18, 2);

        // Inventory.ProductId is unique due to the 1-1 relationship.

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
        new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Quản trị hệ thống"
        },
        new Role
        {
            Id = 2,
            Name = "Customer",
            Description = "Khách hàng"
        }
);

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Membrane",
                Description = "Bàn phím màng",
                CreatedAt = new DateTimeOffset(
                2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Category
            {
                Id = 2,
                Name = "Mechanical",
                Description = "Bàn phím cơ",
                CreatedAt = new DateTimeOffset(
            2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Category
            {
                Id = 3,
                Name = "Optical",
                Description = "Bàn phím quang học",
                CreatedAt = new DateTimeOffset(
            2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Category
            {
                Id = 4,
                Name = "Hall Effect",
                Description = "Bàn phím Hall Effect",
                CreatedAt = new DateTimeOffset(
            2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Category
            {
                Id = 5,
                Name = "TMR",
                Description = "Bàn phím cảm biến TMR",
                CreatedAt = new DateTimeOffset(
            2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Category
            {
                Id = 6,
                Name = "Low-profile",
                Description = "Bàn phím cấu hình thấp",
                CreatedAt = new DateTimeOffset(
            2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }
        );

    }
}
