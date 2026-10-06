using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.DAL.Data;

public class KbsDbContext(DbContextOptions<KbsDbContext> options) : DbContext(options)
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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(category => category.Description)
                .HasColumnType("text");

            entity.Property(category => category.CreatedAt)
                .HasDefaultValueSql("now()");

            entity.Property(category => category.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(category => category.Name)
                .IsUnique()
                .HasDatabaseName("UQ_Categories_Name");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");

            entity.HasKey(brand => brand.Id);

            entity.Property(brand => brand.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(brand => brand.Description)
                .HasColumnType("text");

            entity.Property(brand => brand.CreatedAt)
                .HasDefaultValueSql("now()");

            entity.Property(brand => brand.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(brand => brand.Name)
                .IsUnique()
                .HasDatabaseName("UQ_Brands_Name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", table =>
            {
                table.HasCheckConstraint("CK_Products_Price", "\"Price\" >= 0");
                table.HasCheckConstraint("CK_Products_OriginalPrice", "\"OriginalPrice\" IS NULL OR \"OriginalPrice\" >= 0");
            });

            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(product => product.Price)
                .HasPrecision(12, 2);

            entity.Property(product => product.OriginalPrice)
                .HasPrecision(12, 2);

            entity.Property(product => product.Description)
                .HasColumnType("text");

            entity.Property(product => product.SwitchType)
                .HasMaxLength(50);

            entity.Property(product => product.Layout)
                .HasMaxLength(20);

            entity.Property(product => product.ConnectionType)
                .HasMaxLength(30);

            entity.Property(product => product.Keycap)
                .HasMaxLength(50);

            entity.Property(product => product.Color)
                .HasMaxLength(50);

            entity.Property(product => product.IsHotSwap)
                .HasDefaultValue(false);

            entity.Property(product => product.IsRGB)
                .HasDefaultValue(false);

            entity.Property(product => product.IsActive)
                .HasDefaultValue(true);

            entity.Property(product => product.CreatedAt)
                .HasDefaultValueSql("now()");

            entity.HasIndex(product => product.Name)
                .IsUnique()
                .HasDatabaseName("UQ_Products_Name");

            entity.HasIndex(product => product.CategoryId)
                .HasDatabaseName("IX_Products_CategoryId");

            entity.HasIndex(product => product.BrandId)
                .HasDatabaseName("IX_Products_BrandId");

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Products_Categories");

            entity.HasOne(product => product.Brand)
                .WithMany(brand => brand.Products)
                .HasForeignKey(product => product.BrandId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Products_Brands");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventories", table =>
            {
                table.HasCheckConstraint("CK_Inventories_StockQuantity", "\"StockQuantity\" >= 0");
            });

            entity.HasKey(inventory => inventory.Id);

            entity.Property(inventory => inventory.StockQuantity)
                .HasDefaultValue(0);

            entity.Property(inventory => inventory.UpdatedAt)
                .HasDefaultValueSql("now()");

            entity.HasIndex(inventory => inventory.ProductId)
                .IsUnique()
                .HasDatabaseName("UQ_Inventories_ProductId");

            entity.HasOne(inventory => inventory.Product)
                .WithOne(product => product.Inventory)
                .HasForeignKey<Inventory>(inventory => inventory.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Inventories_Products");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(role => role.Id);

            entity.Property(role => role.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(role => role.Description)
                .HasColumnType("text");

            entity.HasIndex(role => role.Name)
                .IsUnique()
                .HasDatabaseName("UQ_Roles_Name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(user => user.FullName)
                .HasMaxLength(100);

            entity.Property(user => user.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(user => user.CreatedAt)
                .HasDefaultValueSql("now()");

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(user => user.Username)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Username");

            entity.HasIndex(user => user.Email)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");

            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", table =>
            {
                table.HasCheckConstraint("CK_Orders_PaymentMethod", "\"PaymentMethod\" IN ('COD', 'BANK_TRANSFER')");
                // Trạng thái thanh toán: PENDING (chờ xử lý), PAID (đã thanh toán), CANCELLED (đã hủy)
                table.HasCheckConstraint("CK_Orders_PaymentStatus", "\"PaymentStatus\" IN ('PENDING', 'PAID', 'CANCELLED')");
                table.HasCheckConstraint("CK_Orders_OrderStatus", "\"OrderStatus\" IN ('PENDING', 'CONFIRMED', 'SHIPPED', 'COMPLETED', 'CANCELLED')");
            });

            entity.HasKey(order => order.Id);

            entity.Property(order => order.CustomerName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(order => order.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(order => order.ShippingAddress)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(order => order.TotalPrice)
                .HasPrecision(12, 2);

            entity.Property(order => order.PaymentMethod)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("COD");

            entity.Property(order => order.PaymentStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");

            entity.Property(order => order.OrderStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");

            entity.Property(order => order.CreatedAt)
                .HasDefaultValueSql("now()");

            entity.HasIndex(order => order.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            entity.HasIndex(order => order.CreatedAt)
                .HasDatabaseName("IX_Orders_CreatedAt");

            entity.HasIndex(order => order.OrderStatus)
                .HasDatabaseName("IX_Orders_OrderStatus");

            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails", table =>
            {
                table.HasCheckConstraint("CK_OrderDetails_Quantity", "\"Quantity\" > 0");
            });

            entity.HasKey(orderDetail => orderDetail.Id);

            entity.Property(orderDetail => orderDetail.UnitPrice)
                .HasPrecision(12, 2);

            entity.HasIndex(orderDetail => orderDetail.OrderId)
                .HasDatabaseName("IX_OrderDetails_OrderId");

            entity.HasIndex(orderDetail => orderDetail.ProductId)
                .HasDatabaseName("IX_OrderDetails_ProductId");

            entity.HasOne(orderDetail => orderDetail.Order)
                .WithMany(order => order.OrderDetails)
                .HasForeignKey(orderDetail => orderDetail.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderDetails_Orders");

            entity.HasOne(orderDetail => orderDetail.Product)
                .WithMany(product => product.OrderDetails)
                .HasForeignKey(orderDetail => orderDetail.ProductId)
                .HasConstraintName("FK_OrderDetails_Products");
        });
    }
}
