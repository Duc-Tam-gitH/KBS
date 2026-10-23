using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Brands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController(IBrandService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public Task<List<BrandResponseDto>> GetAll(CancellationToken ct) => service.GetAllAsync(ct);

    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<BrandResponseDto>> Get(long id, CancellationToken ct)
    {
        var brand = await service.GetByIdAsync(id, ct);
        return brand is null ? NotFound() : Ok(brand);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<ActionResult<BrandResponseDto>> Create(
        BrandCreateDto dto,
        CancellationToken ct)
    {
        try
        {
            var brand = await service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id = brand.Id }, brand);
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

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<BrandResponseDto>> Update(
        long id,
        BrandUpdateDto dto,
        CancellationToken ct)
    {
        try
        {
            var brand = await service.UpdateAsync(id, dto, ct);
            return brand is null ? NotFound() : Ok(brand);
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
}
