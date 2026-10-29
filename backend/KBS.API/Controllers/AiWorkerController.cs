using System.Security.Cryptography;
using System.Text;
using KBS.BLL.Services;
using KBS.Models.DTOs.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KBS.API.Controllers;

// Dedicated worker credential grants only auto-approval endpoints, never administrator APIs.
public class AiWorkerKeyFilter(IConfiguration configuration) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = configuration["Ai:WorkerKey"];
        var supplied = context.HttpContext.Request.Headers["X-AI-Key"].ToString();
        if (string.IsNullOrWhiteSpace(expected) || expected.Length < 32 || supplied.Length > 512
            || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            context.Result = new UnauthorizedResult();
    }
}

[ApiController]
[Route("api/ai-worker/auto")]
[TypeFilter(typeof(AiWorkerKeyFilter))]
[TypeFilter(typeof(StaffOperationFilter))]
public class AiWorkerController(AiManagementService service) : ControllerBase
{
    [HttpPost("poll")]
    public Task<AiPollDto> Poll(CancellationToken ct) => service.PollAsync(ct);

    [HttpPost("orders/{id:long}/decision")]
    public Task<AiDecisionResponseDto> Decide(
        long id,
        AiDecisionRequest dto,
        CancellationToken ct) => service.DecideAsync(id, dto.Decision, ct);
}
