using KBS.Models.DTOs.Dashboard;
namespace KBS.BLL.Interfaces;
public interface IDashboardService { Task<DashboardResponseDto> GetAsync(CancellationToken ct = default); }
