
using KBS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using KBS.BLL.Interfaces;
using KBS.BLL.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using KBS.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký các dịch vụ cho ứng dụng.
builder.Services.AddControllers();

// Đăng ký các dịch vụ tùy chỉnh.
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

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

// Cấu hình xác thực JWT.
var jwtSettings = builder.Configuration.GetSection("Jwt");

var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrWhiteSpace(issuer) ||
    string.IsNullOrWhiteSpace(audience) ||
    string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "Cấu hình Jwt chưa đầy đủ trong appsettings.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)),

            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });


var app = builder.Build();

// Cấu hình pipeline xử lý request.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
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

await AdminSeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.Run();
