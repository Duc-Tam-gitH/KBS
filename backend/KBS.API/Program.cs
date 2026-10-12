
using KBS.DAL.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký các dịch vụ cho ứng dụng.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Đăng ký kết nối PostgreSQL và DbContext.
var connectionString = builder.Configuration.GetConnectionString("KBSConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:KBSConnection trong User Secrets.");
}

builder.Services.AddDbContext<KbsDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Cấu hình pipeline xử lý request.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health/database", async (KbsDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    return canConnect
        ? Results.Ok(new
        {
            status = "connected",
            database = "KBS_DB"
        })
        : Results.Problem(
            "Không thể kết nối PostgreSQL. Hãy kiểm tra User Secrets và PostgreSQL.");
});

app.Run();
