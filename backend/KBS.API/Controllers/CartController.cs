using System.Security.Claims;
using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController(ICartService service) : ControllerBase
{
    [HttpGet]
    public Task<CartResponseDto> Get(CancellationToken ct) => service.GetAsync(UserId(), ct);

    [HttpPost("items")]
    public async Task<ActionResult<CartResponseDto>> Add(
        AddCartItemDto dto,
        CancellationToken ct)
    {
        try
        {
            return Ok(await service.AddAsync(UserId(), dto, ct));
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return Conflict(new { message = e.Message });
        }
    }

    [HttpPut("items/{productId:long}")]
    public async Task<ActionResult<CartResponseDto>> Update(
        long productId,
        UpdateCartItemDto dto,
        CancellationToken ct)
    {
        try
        {
            var cart = await service.UpdateAsync(UserId(), productId, dto, ct);
            return cart is null ? NotFound() : Ok(cart);
        }
        catch (InvalidOperationException e)
        {
            return Conflict(new { message = e.Message });
        }
    }

    [HttpDelete("items/{productId:long}")]
    public async Task<IActionResult> Remove(long productId, CancellationToken ct)
    {
        return await service.RemoveAsync(UserId(), productId, ct) ? NoContent() : NotFound();
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
