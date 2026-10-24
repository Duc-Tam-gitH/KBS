using System.Security.Claims;
using KBS.BLL.Services;
using KBS.Models.DTOs.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Roles = "Customer,Staff,Admin")]
[TypeFilter(typeof(StaffOperationFilter))]
public class ChatController(ChatService service) : ControllerBase
{
    [HttpGet("conversations")]
    public Task<List<ChatConversationDto>> List(CancellationToken ct) => service.ListAsync(UserId(), Role(), ct);

    [HttpPost("conversations")]
    public async Task<IActionResult> Open([FromQuery] long? customerId, CancellationToken ct)
    {
        var id = await service.OpenAsync(UserId(), Role(), customerId, ct);
        return Ok(new { id });
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPut("conversations/{id:long}/claim")]
    public async Task<IActionResult> Claim(long id, CancellationToken ct)
    {
        await service.ClaimAsync(id, UserId(), ct);
        return NoContent();
    }

    [HttpGet("conversations/{id:long}/messages")]
    public Task<List<ChatMessageDto>> Messages(
        long id,
        [FromQuery] long? beforeId,
        [FromQuery] long? afterId,
        CancellationToken ct) => service.MessagesAsync(id, UserId(), Role(), beforeId, afterId, ct);

    [HttpPost("conversations/{id:long}/messages")]
    public async Task<IActionResult> Send(
        long id,
        ChatSendRequest dto,
        CancellationToken ct)
    {
        await service.SendAsync(id, UserId(), Role(), dto.Content, ct);
        return NoContent();
    }

    [HttpPut("conversations/{id:long}/read")]
    public async Task<IActionResult> Read(
        long id,
        ChatReadRequest dto,
        CancellationToken ct)
    {
        await service.ReadAsync(id, UserId(), Role(), dto.ThroughMessageId, ct);
        return NoContent();
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role() => User.FindFirstValue(ClaimTypes.Role)!;
}
