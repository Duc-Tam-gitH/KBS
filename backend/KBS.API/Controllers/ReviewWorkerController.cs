using System.Security.Claims;
using KBS.BLL.Services;
using KBS.Models.DTOs.Ai;
using KBS.Models.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/ai-worker/reviewnpl")]
[TypeFilter(typeof(AiWorkerKeyFilter))]
[TypeFilter(typeof(StaffOperationFilter))]
public class ReviewWorkerController(ReviewModerationService service) : ControllerBase
{
    [HttpPost("poll")]
    public Task<ReviewPoll> Poll(CancellationToken ct) => service.PollAsync(ct);

    [HttpPost("reviews/{id:long}/decision")]
    public Task<ReviewDecisionResult> Decide(
        long id,
        ReviewWorkerDecision dto,
        CancellationToken ct) => service.WorkerAsync(id, dto, ct);
}

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin,Staff")]
[TypeFilter(typeof(StaffOperationFilter))]
public class ReviewModerationController(ReviewModerationService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<ModerationReview>> List(
        CancellationToken ct,
        string? status = null,
        int page = 1) => service.ListAsync(status, page, ct);

    [HttpPut("{id:long}/moderation")]
    public Task<ReviewDecisionResult> Moderate(
        long id,
        ReviewStaffDecision dto,
        CancellationToken ct) => service.ModerateAsync(id, dto.Status,
            long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
}
