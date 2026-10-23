using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Likelist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/likelist")]
[Authorize]
public class LikelistController(ILikelistService service) : ControllerBase
{
    [HttpGet]
    public Task<List<LikelistItemResponseDto>> Get(CancellationToken ct) => service.GetAsync(UserId(), ct);

    [HttpPut("items/{productId:long}")]
    public async Task<IActionResult> Add(
        [Range(1, long.MaxValue)] long productId,
        CancellationToken ct)
    {
        return await service.AddAsync(UserId(), productId, ct)
            ? NoContent()
            : NotFound(new { message = "Sản phẩm không tồn tại hoặc đã ngừng bán." });
    }

    [HttpDelete("items/{productId:long}")]
    public async Task<IActionResult> Remove(
        [Range(1, long.MaxValue)] long productId,
        CancellationToken ct)
    {
        return await service.RemoveAsync(UserId(), productId, ct) ? NoContent() : NotFound();
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
