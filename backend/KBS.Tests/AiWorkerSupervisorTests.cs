using System.Diagnostics;
using KBS.API.Controllers;
using KBS.API.Services;
using KBS.BLL.Services;
using KBS.DAL.Data;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KBS.Tests;

public class AiWorkerSupervisorTests
{
    [Fact]
    public async Task Real_python_worker_connects_to_api_and_applies_order_rules()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ai:WorkerKey"] = "test-real-worker-key-at-least-32-characters",
            ["Ai:Workers:ai_auto"] = "ai_auto.py",
            ["Ai:Workers:ai_recommender"] = "ai_recommender.py"
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Services.AddControllers().AddApplicationPart(typeof(AiWorkerController).Assembly);
        builder.Services.AddScoped<AiManagementService>();
        builder.Services.AddScoped<RecommendationService>();
        builder.Services.AddSingleton<RecommendationCache>();
        builder.Services.AddHostedService<AiWorkerSupervisor>();
        var databaseName = "ai-network-test-" + Guid.NewGuid();
        builder.Services.AddDbContext<KbsDbContext>(options => options
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        await using var app = builder.Build();
        app.MapControllers();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            await db.Database.EnsureCreatedAsync();
            var module = await db.AiModules.SingleAsync(x => x.Id == "ai_auto");
            module.IsEnabled = true;
            var recommender = await db.AiModules.SingleAsync(x => x.Id == "ai_recommender");
            recommender.IsEnabled = true;
            var product = new Product
            {
                Name = "Worker recommendation test", Sound = "Thocky", SwitchType = "Linear",
                Category = new Category { Name = "Worker category" },
                Brand = new Brand { Name = "Worker brand", IsActive = true },
                Inventory = new Inventory { StockQuantity = 2 }
            };
            db.Products.Add(product);
            var purchased = new Product
            {
                Name = "Previously purchased", Sound = "Thocky", SwitchType = "Linear",
                Category = product.Category, Brand = product.Brand, IsActive = false,
                Inventory = new Inventory { StockQuantity = 0 }
            };
            db.Orders.Add(new Order
            {
                UserId = 77, PaymentMethod = "COD", OrderStatus = "COMPLETED", TotalPrice = 3000,
                OrderDetails = [new OrderDetail { Product = purchased, Quantity = 1, UnitPrice = 3000 }]
            });
            db.Orders.AddRange(new Order { PaymentMethod = "COD", TotalPrice = 1000 },
                new Order { PaymentMethod = "COD", TotalPrice = 10000000 });
            await db.SaveChangesAsync();
        }
        await app.StartAsync();
        try
        {
            await Until(async () =>
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
                return await db.AiEvents.CountAsync(x => x.OrderId != null) == 2
                    && await db.AiEvents.AnyAsync(x => x.Kind == "RECOMMENDED");
            });
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            Assert.Equal("CONFIRMED", (await db.Orders.SingleAsync(x => x.TotalPrice == 1000)).OrderStatus);
            Assert.Equal("PENDING", (await db.Orders.SingleAsync(x => x.TotalPrice == 10000000)).OrderStatus);
            Assert.Contains(await db.AiEvents.ToListAsync(), x => x.Kind == "MANUAL_REVIEW");
            Assert.NotNull((await db.AiModules.SingleAsync(x => x.Id == "ai_auto")).LastHeartbeatAt);
            var recommendations = scope.ServiceProvider.GetRequiredService<RecommendationService>();
            Assert.Single((await recommendations.GetAsync(null, CancellationToken.None)).Items);
            var personal = await recommendations.GetAsync(77, CancellationToken.None);
            Assert.Equal("PERSONALIZED", personal.Mode);
            Assert.Equal("Worker recommendation test", Assert.Single(personal.Items).Name);
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public async Task Workers_follow_module_state_report_failures_and_stop_with_backend()
    {
        using var app = new TestApiFactory();
        using var client = app.CreateClient();
        var directory = Path.Combine(Path.GetTempPath(), "kbs-ai-test-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var script = """
            import os, pathlib, sys, time
            name = pathlib.Path(sys.argv[0]).stem
            assert len(os.environ['KBS_AI_WORKER_KEY']) >= 32
            assert os.environ['KBS_API_URL'] == 'http://localhost:5231'
            with open(name + '.starts', 'a') as f:
                f.write(str(os.getpid()) + '\n')
            while True:
                time.sleep(0.1)
            """;
        await File.WriteAllTextAsync(Path.Combine(directory, "ai_auto.py"), script);
        await File.WriteAllTextAsync(Path.Combine(directory, "ai_second.py"), script);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ai:WorkerDirectory"] = directory,
            ["Ai:WorkerKey"] = "test-worker-key-at-least-32-characters",
            ["Ai:ApiUrl"] = "http://localhost:5231",
            ["Ai:Workers:ai_auto"] = "ai_auto.py",
            ["Ai:Workers:ai_second"] = "ai_second.py",
            ["Ai:Workers:ai_missing"] = "missing.py"
        }).Build();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            db.AiModules.AddRange(
                new AiModule { Id = "ai_second", Name = "Second test worker", IsEnabled = true },
                new AiModule { Id = "ai_missing", Name = "Missing test worker", IsIntegrated = true,
                    IsEnabled = true });
            await db.SaveChangesAsync();
        }
        using var supervisor = new AiWorkerSupervisor(app.Services.GetRequiredService<IServiceScopeFactory>(),
            configuration, app.Services.GetRequiredService<IServer>(),
            app.Services.GetRequiredService<ILogger<AiWorkerSupervisor>>());
        var autoStarts = Path.Combine(directory, "ai_auto.starts");
        var secondStarts = Path.Combine(directory, "ai_second.starts");
        var pids = new List<int>();
        try
        {
            await supervisor.StartAsync(CancellationToken.None);
            await Until(async () => await ErrorCount() == 1);
            Assert.False(File.Exists(autoStarts)); // Disabled module is not started.
            Assert.False(File.Exists(secondStarts)); // Unintegrated module is not started.
            await SetModule("ai_auto", true);
            await SetModule("ai_second", true);
            await Until(async () =>
            {
                using var scope = app.Services.CreateScope();
                var errors = await scope.ServiceProvider.GetRequiredService<KbsDbContext>().AiEvents
                    .Where(x => x.ModuleId != "ai_missing" && x.Kind == "ERROR").Select(x => x.Message).ToListAsync();
                Assert.True(errors.Count == 0, string.Join("\n", errors));
                return File.Exists(autoStarts) && File.Exists(secondStarts);
            });
            pids.Add(int.Parse(File.ReadAllText(autoStarts).Trim()));
            pids.Add(int.Parse(File.ReadAllText(secondStarts).Trim()));
            await Task.Delay(2300);
            Assert.Single(File.ReadAllLines(autoStarts)); // Repeated sync does not duplicate workers.
            Assert.Equal(1, await ErrorCount()); // Broken startup does not flood the event log.
            await SetModule("ai_auto", false);
            await Until(() => Task.FromResult(Exited(pids[0])));
            Assert.False(Exited(pids[1])); // OFF affects only the selected worker.
            await SetModule("ai_auto", true);
            await Until(() => Task.FromResult(File.ReadAllLines(autoStarts).Length == 2));
            pids.Add(int.Parse(File.ReadAllLines(autoStarts)[1]));
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
            Assert.All(pids, pid => Assert.True(Exited(pid))); // Host shutdown cleans up all children.
            Directory.Delete(directory, recursive: true);
        }

        async Task SetModule(string id, bool enabled)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
            var module = await db.AiModules.SingleAsync(x => x.Id == id);
            module.IsIntegrated = true;
            module.IsEnabled = enabled;
            module.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        async Task<int> ErrorCount()
        {
            using var scope = app.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<KbsDbContext>().AiEvents
                .CountAsync(x => x.ModuleId == "ai_missing" && x.Kind == "ERROR");
        }
    }

    private static bool Exited(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.HasExited;
        }
        catch (ArgumentException) { return true; }
    }

    private static async Task Until(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!await condition()) await Task.Delay(100, timeout.Token);
    }
}
