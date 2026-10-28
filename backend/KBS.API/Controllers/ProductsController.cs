using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Common;
using KBS.Models.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService service) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("management")]
    public Task<List<ProductResponseDto>> GetForManagement(CancellationToken ct)
        => service.GetForManagementAsync(ct);

    [AllowAnonymous]
    [HttpGet]
    public Task<PagedResponse<ProductResponseDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] long? categoryId,
        [FromQuery] long? brandId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => service.GetAllAsync(search, categoryId, brandId, minPrice, maxPrice, page, pageSize, ct);

    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductResponseDto>> Get(long id, CancellationToken ct)
    {
        var x = await service.GetByIdAsync(id, ct);
        return x is null ? NotFound() : Ok(x);
    }

    [Authorize(Roles = "Admin,Staff,Nhân viên")]
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create(ProductCreateDto dto, CancellationToken ct)
    {
        try
        {
            var x = await service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id = x.Id }, x);
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff,Nhân viên")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProductResponseDto>> Update(long id, ProductUpdateDto dto, CancellationToken ct)
    {
        try
        {
            var x = await service.UpdateAsync(id, dto, ct);
            return x is null ? NotFound() : Ok(x);
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }
}
