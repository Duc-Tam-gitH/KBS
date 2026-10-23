
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
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<LikelistItem> Likelist => Set<LikelistItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LikelistItem>().ToTable("Likelist");
        modelBuilder.Entity<LikelistItem>()
            .HasIndex(x => new { x.UserId, x.ProductId })
            .IsUnique();
        modelBuilder.Entity<LikelistItem>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LikelistItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Một danh mục có nhiều sản phẩm
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductImage>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductImage>()
            .HasIndex(i => new { i.ProductId, i.SortOrder });

        // Một thương hiệu có nhiều sản phẩm
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Mỗi sản phẩm có một bản ghi tồn kho
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Inventory)
            .WithOne(i => i.Product)
            .HasForeignKey<Inventory>(i => i.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Một vai trò có nhiều người dùng
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Một người dùng có nhiều đơn hàng
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Một đơn hàng có nhiều chi tiết đơn hàng
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.User)
            .WithMany(u => u.CartItems)
            .HasForeignKey(ci => ci.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(ci => ci.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Một sản phẩm có thể nằm trong nhiều chi tiết đơn hàng
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Product)
            .WithMany(p => p.OrderDetails)
            .HasForeignKey(od => od.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Các ràng buộc duy nhất
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasFilter("\"PhoneNumber\" IS NOT NULL");

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<CartItem>()
            .HasIndex(ci => new { ci.UserId, ci.ProductId })
            .IsUnique();

        // Độ chính xác số thập phân cho giá trị tiền
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

        // Inventory.ProductId là duy nhất do quan hệ một-một.

        // Khởi tạo dữ liệu vai trò
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

        // Khởi tạo dữ liệu danh mục
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
