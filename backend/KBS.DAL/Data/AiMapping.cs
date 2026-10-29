using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.DAL.Data;

internal static class AiMapping
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        var module = modelBuilder.Entity<AiModule>();
        module.Property(x => x.Id).HasMaxLength(50);
        module.Property(x => x.Name).HasMaxLength(150);
        module.Property(x => x.Description).HasMaxLength(1000);
        module.Property(x => x.SourceFile).HasMaxLength(100);
        module.Property(x => x.ApprovalLimit).HasPrecision(18, 2);
        module.ToTable("AiModules", table =>
        {
            table.HasCheckConstraint("CK_AiModules_Enabled", "NOT \"IsEnabled\" OR \"IsIntegrated\"");
            table.HasCheckConstraint("CK_AiModules_Limit", "\"ApprovalLimit\" IS NULL OR \"ApprovalLimit\" > 0");
        });
        var created = new DateTimeOffset(2026, 10, 29, 0, 0, 0, TimeSpan.Zero);
        module.HasData(
            new AiModule { Id = "ai_auto", Name = "Tự động xác nhận đơn", SourceFile = "ai_auto.py",
                Description = "Xác nhận đơn chờ dưới 10 triệu; từ 10 triệu cần staff xác nhận.",
                IsIntegrated = true, ApprovalLimit = 10000000, UpdatedAt = created },
            new AiModule { Id = "ai_anti", Name = "Phát hiện mua gom", SourceFile = "ai_anti.py",
                Description = "Phát hiện mua trên 100 sản phẩm trong 24 giờ.", UpdatedAt = created },
            new AiModule { Id = "ai_security", Name = "Giám sát đơn hàng", SourceFile = "ai_security.py",
                Description = "Phát hiện giá trị đơn hàng bất thường.", UpdatedAt = created },
            new AiModule { Id = "ai_reviewnpl", Name = "Kiểm tra đánh giá", SourceFile = "ai_reviewnpl.py",
                Description = "Lọc từ khóa và phát hiện spam đánh giá.", UpdatedAt = created },
            new AiModule { Id = "ai_recommender", Name = "Gợi ý sản phẩm", SourceFile = "ai_recommender.py",
                Description = "Gợi ý sản phẩm theo kịch bản mua hàng.", UpdatedAt = created });
        var activity = modelBuilder.Entity<AiEvent>();
        activity.Property(x => x.ModuleId).HasMaxLength(50);
        activity.Property(x => x.Kind).HasMaxLength(40);
        activity.Property(x => x.Message).HasMaxLength(1000);
        activity.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);
        activity.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        activity.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        activity.HasIndex(x => new { x.ModuleId, x.Id });
        activity.HasIndex(x => new { x.ModuleId, x.OrderId, x.Kind }).IsUnique();
    }
}
