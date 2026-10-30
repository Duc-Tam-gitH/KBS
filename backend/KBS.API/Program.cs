
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
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Dữ liệu không hợp lệ." : e.ErrorMessage).ToArray());
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { message = "Vui lòng kiểm tra lại dữ liệu nhập.", errors });
    });
builder.Services.AddAuthorization();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("Chưa cấu hình Cors:AllowedOrigins.");
if (allowedOrigins.Length == 0 || allowedOrigins.Any(string.IsNullOrWhiteSpace))
{
    throw new InvalidOperationException("Cors:AllowedOrigins không hợp lệ.");
}
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Đăng ký các dịch vụ tùy chỉnh.
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ILikelistService, LikelistService>();
builder.Services.AddScoped<ProductReviewService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<AiManagementService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ShopSettingsService>();

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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var db = context.HttpContext.RequestServices.GetRequiredService<KbsDbContext>();
                if (!long.TryParse(idText, out var id)
                    || !await db.Users.AnyAsync(x => x.Id == id && x.IsActive && x.Role!.Name == role,
                        context.HttpContext.RequestAborted))
                {
                    context.Fail("Tài khoản đã bị khóa hoặc quyền truy cập đã thay đổi.");
                }
            }
        };
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

app.UseCors("Frontend");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimEnd('/');
    var publicEntry = string.Equals(path, "/api/auth/login", StringComparison.OrdinalIgnoreCase)
        || context.Request.Path.StartsWithSegments("/api/shop");
    if (context.Request.Path.StartsWithSegments("/api") && !publicEntry
        && !HttpMethods.IsOptions(context.Request.Method) && !context.User.IsInRole("Admin"))
    {
        var db = context.RequestServices.GetRequiredService<KbsDbContext>();
        var isOpen = await db.ShopSettings.AsNoTracking().Select(x => (bool?)x.IsOpen)
            .SingleOrDefaultAsync(context.RequestAborted) ?? true;
        if (!isOpen)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                code = "SITE_CLOSED", message = "Website đang tạm đóng để bảo trì. Vui lòng quay lại sau."
            });
            return;
        }
    }
    await next();
});
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

public partial class Program { }
