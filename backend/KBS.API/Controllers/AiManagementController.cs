using System.Security.Claims;
using KBS.BLL.Services;
using KBS.Models.DTOs.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/admin/ai")]
[Authorize(Roles = "Admin,Staff")]
[TypeFilter(typeof(StaffOperationFilter))]
public class AiManagementController(AiManagementService service) : ControllerBase
{
    [HttpGet("modules")]
    public Task<List<AiModuleResponseDto>> Modules(CancellationToken ct) => service.ModulesAsync(ct);

    [HttpPut("modules/{id}/enabled")]
    [Authorize(Roles = "Admin")]
    public Task<AiModuleResponseDto> Enable(
        string id,
        AiEnabledRequest dto,
        CancellationToken ct) =>
        service.EnableAsync(id, dto.IsEnabled!.Value, long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);

    [HttpGet("events")]
    public Task<AiEventsPage> Events(
        CancellationToken ct,
        string? moduleId = null,
        long? beforeId = null,
        int limit = 50) => service.EventsAsync(moduleId, beforeId, limit, ct);
}
