using KBS.BLL.Services;
using KBS.Models.DTOs.Ai;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[TypeFilter(typeof(StaffOperationFilter))]
public class RecommendationsController(RecommendationService service) : ControllerBase
{
    [HttpGet]
    public Task<RecommendationPage> Get(CancellationToken ct = default) =>
        service.GetAsync(User.Identity?.IsAuthenticated == true
            && long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null, ct);
}

[ApiController]
[Route("api/ai-worker/recommender")]
[TypeFilter(typeof(AiWorkerKeyFilter))]
[TypeFilter(typeof(StaffOperationFilter))]
public class RecommenderWorkerController(RecommendationService service) : ControllerBase
{
    [HttpPost("poll")]
    public Task<RecommendationPoll> Poll(CancellationToken ct) => service.PollAsync(ct);

    [HttpPost("results")]
    public async Task<IActionResult> Publish(RecommendationUpdate dto, CancellationToken ct)
    {
        await service.PublishAsync(dto, ct);
        return Ok(new { result = "RECOMMENDED" });
    }
}
