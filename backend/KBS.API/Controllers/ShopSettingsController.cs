using KBS.BLL.Services;
using KBS.Models.DTOs.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[TypeFilter(typeof(StaffOperationFilter))]
public class ShopSettingsController(ShopSettingsService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("api/shop")]
    public Task<PublicShopDto> Public(CancellationToken ct) => service.PublicAsync(ct);

    [Authorize(Roles = "Admin")]
    [HttpGet("api/admin/shop")]
    public Task<AdminShopDto> Get(CancellationToken ct) => service.AdminAsync(ct);

    [Authorize(Roles = "Admin")]
    [HttpPut("api/admin/shop")]
    public Task<AdminShopDto> Save(SaveShopSettingsDto dto, CancellationToken ct) => service.SaveAsync(dto, ct);

    [AllowAnonymous]
    [HttpGet("api/shop/media/{kind}")]
    public async Task<IActionResult> Media(string kind, CancellationToken ct)
    {
        var media = await service.MediaAsync(kind, ct);
        if (media == null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-cache";
        return File(media.Value.Data, media.Value.ContentType);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("api/admin/shop/media/{kind}")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        string kind,
        IFormFile file,
        CancellationToken ct)
    {
        if (file.Length == 0 || file.Length > ShopSettingsService.MediaLimit(kind))
            return BadRequest(new { message = "Logo tối đa 2 MB, banner tối đa 5 MB." });
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        await service.SaveMediaAsync(kind, buffer.ToArray(), file.ContentType, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("api/admin/shop/media/{kind}")]
    public async Task<IActionResult> DeleteMedia(string kind, CancellationToken ct)
    {
        await service.DeleteMediaAsync(kind, ct);
        return NoContent();
    }
}
