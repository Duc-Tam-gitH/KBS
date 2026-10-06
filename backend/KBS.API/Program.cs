using KBS.DAL.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký các dịch vụ cho ứng dụng.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<KbsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
// Cấu hình OpenAPI cho môi trường phát triển.
builder.Services.AddOpenApi();

var app = builder.Build();

// Cấu hình pipeline xử lý request.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Tự động tạo database và áp dụng migration khi API khởi động
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KbsDbContext>();

    // Migrate() sẽ tạo database nếu chưa tồn tại,
    // và áp dụng tất cả migration đang chờ
    dbContext.Database.Migrate();
}

app.Run();
