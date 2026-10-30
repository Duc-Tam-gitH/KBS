using System.Diagnostics;
using KBS.DAL.Data;
using KBS.Models.Entities;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;

namespace KBS.API.Services;

public sealed class AiWorkerSupervisor(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    IServer server,
    ILogger<AiWorkerSupervisor> logger) : BackgroundService
{
    private readonly Dictionary<string, Process> processes = new();
    private readonly Dictionary<string, DateTimeOffset> failed = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield before querying the DB so server startup is never blocked by a worker.
        await Task.Yield();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SynchronizeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception error)
                {
                    logger.LogError(error, "Cannot synchronize AI workers.");
                }
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            foreach (var process in processes.Values) Stop(process);
            processes.Clear();
        }
    }

    private async Task SynchronizeAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KbsDbContext>();
        var modules = await db.AiModules.AsNoTracking().ToListAsync(ct);
        var enabled = modules.Where(x => x.IsEnabled && x.IsIntegrated).Select(x => x.Id).ToHashSet();
        foreach (var id in failed.Keys.Where(id => !enabled.Contains(id)).ToArray()) failed.Remove(id);
        foreach (var id in processes.Keys.Where(id => !enabled.Contains(id)).ToArray())
        {
            Stop(processes[id]);
            processes.Remove(id);
            logger.LogInformation("Stopped AI worker {Module}.", id);
        }

        foreach (var module in modules.Where(x => enabled.Contains(x.Id)))
        {
            if (failed.TryGetValue(module.Id, out var failedAt) && failedAt == module.UpdatedAt) continue;
            try
            {
                if (processes.TryGetValue(module.Id, out var current))
                {
                    if (!current.HasExited) continue;
                    var exitCode = current.ExitCode;
                    current.Dispose();
                    processes.Remove(module.Id);
                    throw new InvalidOperationException($"Worker dừng với mã lỗi {exitCode}. Xem log backend.");
                }
                var url = ApiUrl();
                if (url == null) continue; // Kestrel has not started listening yet.
                var process = StartWorker(module.Id, url);
                processes.Add(module.Id, process);
                logger.LogInformation("Started AI worker {Module}, PID {Pid}.", module.Id, process.Id);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                failed[module.Id] = module.UpdatedAt; // Retry after OFF/ON; avoid flooding events.
                logger.LogError(error, "AI worker {Module} failed.", module.Id);
                db.AiEvents.Add(new AiEvent
                {
                    ModuleId = module.Id, Kind = "ERROR",
                    Message = $"Không chạy được worker: {error.Message} Tắt rồi bật lại AI sau khi sửa lỗi."
                });
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private Process StartWorker(string id, string url)
    {
        // Executables come only from trusted server config, never SourceFile in DB or browser input.
        var script = configuration[$"Ai:Workers:{id}"];
        if (string.IsNullOrWhiteSpace(script) || Path.GetFileName(script) != script
            || !script.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Chưa cấu hình file Python hợp lệ tại Ai:Workers:{id}.");
        var directory = configuration["Ai:WorkerDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "ai");
        var path = Path.GetFullPath(Path.Combine(directory, script));
        if (!File.Exists(path)) throw new InvalidOperationException($"Thiếu file worker {script}.");
        var key = configuration["Ai:WorkerKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("Ai:WorkerKey phải có ít nhất 32 ký tự.");
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(profile))
            profile = Environment.GetEnvironmentVariable(OperatingSystem.IsWindows() ? "USERPROFILE" : "HOME") ?? "";
        var bundled = Path.Combine(profile,
            ".cache", "codex-runtimes", "codex-primary-runtime", "dependencies", "python", "python.exe");
        var python = configuration["Ai:PythonPath"] ?? (File.Exists(bundled) ? bundled : "python");
        var start = new ProcessStartInfo(python)
        {
            WorkingDirectory = Path.GetDirectoryName(path)!, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add("-u");
        start.ArgumentList.Add(path);
        start.Environment["KBS_API_URL"] = url;
        start.Environment["KBS_AI_WORKER_KEY"] = key;
        var process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) logger.LogInformation("AI {Module}: {Output}", id, e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) logger.LogWarning("AI {Module}: {Output}", id, e.Data);
        };
        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process;
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            Stop(process);
            throw new InvalidOperationException("Không chạy được Python. Kiểm tra cấu hình Ai:PythonPath.", error);
        }
        catch
        {
            Stop(process);
            throw;
        }
    }

    private string? ApiUrl()
    {
        if (configuration["Ai:ApiUrl"] is { Length: > 0 } configured) return configured;
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses
            .FirstOrDefault(x => x.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        if (address != null)
        {
            // Connect to this server via loopback, including when Kestrel binds to 0.0.0.0 or [::].
            return $"http://localhost:{new Uri(address).Port}";
        }
        if (server.Features.Get<IServerAddressesFeature>()?.Addresses.Count > 0)
            throw new InvalidOperationException("Cấu hình Ai:ApiUrl với HTTPS tin cậy hoặc mở HTTP loopback cho worker.");
        return null;
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException) { } // Process never started or already exited.
        finally { process.Dispose(); }
    }
}
