using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KBS.BLL.Services;
using KBS.Models.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/products/{productId:long}/reviews")]
public class ProductReviewsController(ProductReviewService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ProductReviewsResponseDto>> Get(
        [Range(1, long.MaxValue)] long productId,
        [FromQuery]
        [Range(1, 5)] int? rating,
        [FromQuery]
        [Range(1, int.MaxValue)] int page = 1,
        CancellationToken ct = default)
    {
        var response = await service.GetAsync(productId, rating, page, ct);
        return response is null ? NotFound() : Ok(response);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [Range(1, long.MaxValue)] long productId,
        ProductReviewCreateDto dto,
        CancellationToken ct)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            return await service.CreateAsync(productId, userId, dto, ct) ? NoContent() : NotFound();
        }
        catch (ArgumentException error)
        {
            return BadRequest(new { message = error.Message });
        }
        catch (InvalidOperationException error)
        {
            return Conflict(new { message = error.Message });
        }
    }
}
