using KBS.BLL.Interfaces; using KBS.Models.DTOs.Dashboard; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace KBS.API.Controllers;
[ApiController][Route("api/admin/dashboard")][Authorize(Roles="Admin")] public class AdminDashboardController(IDashboardService service):ControllerBase { [HttpGet] public Task<DashboardResponseDto> Get(CancellationToken ct)=>service.GetAsync(ct); }
