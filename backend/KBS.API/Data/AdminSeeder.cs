
using KBS.DAL.Data;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KBS.API.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<KbsDbContext>();

        var passwordHasher = new PasswordHasher<User>();

        const long adminRoleId = 1;

        // Kiểm tra Role Admin đã tồn tại chưa.
        var adminRole = await dbContext.Roles
            .SingleOrDefaultAsync(role => role.Id == adminRoleId);

        if (adminRole is null)
        {
            throw new InvalidOperationException(
                "Role Admin chưa tồn tại. Hãy kiểm tra dữ liệu seed Role.");
        }

        // Nếu đã có tài khoản Admin thì không tạo thêm.
        var adminExists = await dbContext.Users
            .AnyAsync(user => user.RoleId == adminRoleId);

        if (adminExists)
        {
            return;
        }

        var username = configuration["AdminBootstrap:Username"];
        var email = configuration["AdminBootstrap:Email"];
        var password = configuration["AdminBootstrap:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Thiếu cấu hình AdminBootstrap:Username, Email hoặc Password.");
        }

        // Tránh xung đột với tài khoản đã tồn tại.
        var accountExists = await dbContext.Users.AnyAsync(user =>
            user.Username == username || user.Email == email);

        if (accountExists)
        {
            throw new InvalidOperationException(
                "Username hoặc Email khởi tạo Admin đã được sử dụng.");
        }

        var admin = new User
        {
            Username = username,
            Email = email,
            FullName = "System Administrator",
            RoleId = adminRoleId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(
            admin,
            password);

        dbContext.Users.Add(admin);

        await dbContext.SaveChangesAsync();
    }
}
