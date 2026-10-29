
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.DAL.Data;

public class KbsDbContext(DbContextOptions<KbsDbContext> options)
    : DbContext(options)
{
    public DbSet<ShopSettings> ShopSettings => Set<ShopSettings>();
    public DbSet<AiModule> AiModules => Set<AiModule>();
    public DbSet<AiEvent> AiEvents => Set<AiEvent>();
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
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ShopSettings>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<ShopSettings>().ToTable("ShopSettings", table =>
            table.HasCheckConstraint("CK_ShopSettings_Singleton", "\"Id\" = 1"));
        AiMapping.Configure(modelBuilder);

        modelBuilder.Entity<Order>().Property(x => x.TrackingNumber).HasMaxLength(100);
        modelBuilder.Entity<Inventory>().Property(x => x.LowStockThreshold).HasDefaultValue(5);
        modelBuilder.Entity<Inventory>().ToTable("Inventories", table =>
        {
            table.HasCheckConstraint("CK_Inventories_Stock", "\"StockQuantity\" >= 0");
            table.HasCheckConstraint("CK_Inventories_Threshold", "\"LowStockThreshold\" >= 0");
        });
        modelBuilder.Entity<StaffShift>().HasOne(x => x.Staff).WithMany()
            .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StaffShift>().HasIndex(x => x.StaffId).IsUnique().HasFilter("\"EndedAt\" IS NULL");
        modelBuilder.Entity<OrderNote>().HasOne(x => x.Order).WithMany()
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderNote>().HasOne(x => x.Staff).WithMany()
            .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderNote>().Property(x => x.Content).HasMaxLength(2000);
        modelBuilder.Entity<OrderNote>().HasIndex(x => new { x.OrderId, x.CreatedAt });
        modelBuilder.Entity<InventoryMovement>().HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryMovement>().HasOne(x => x.Actor).WithMany()
            .HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryMovement>().HasOne(x => x.Order).WithMany()
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryMovement>().Property(x => x.Reason).HasMaxLength(500);
        modelBuilder.Entity<InventoryMovement>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        modelBuilder.Entity<InventoryMovement>().ToTable("InventoryMovements", table =>
            table.HasCheckConstraint("CK_InventoryMovements_Balance",
                "\"QuantityAfter\" >= 0 AND \"QuantityBefore\" >= 0 "
                + "AND \"QuantityAfter\"::bigint = \"QuantityBefore\"::bigint + \"Delta\"::bigint"));
        modelBuilder.Entity<ChatConversation>().HasIndex(x => x.CustomerId).IsUnique();
        modelBuilder.Entity<ChatConversation>().HasOne(x => x.Customer).WithMany()
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChatConversation>().HasOne(x => x.Staff).WithMany()
            .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChatMessage>().HasOne(x => x.Conversation).WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChatMessage>().HasOne(x => x.Sender).WithMany()
            .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChatMessage>().Property(x => x.Content).HasMaxLength(4000);
        modelBuilder.Entity<ChatMessage>().HasIndex(x => new { x.ConversationId, x.Id });

        modelBuilder.Entity<ProductReview>()
            .ToTable("ProductReviews", table =>
                table.HasCheckConstraint("CK_ProductReviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
        modelBuilder.Entity<ProductReview>().Property(x => x.Comment).HasMaxLength(2000);
        modelBuilder.Entity<ProductReview>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        modelBuilder.Entity<ProductReview>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        modelBuilder.Entity<ProductReview>().HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductReview>().HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

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
