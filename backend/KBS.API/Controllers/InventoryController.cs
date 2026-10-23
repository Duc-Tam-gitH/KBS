using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin,Staff")]
public class InventoryController(IInventoryService service) : ControllerBase
{
    [HttpGet("product/{productId:long}")]
    public async Task<ActionResult<InventoryResponseDto>> Get(
        long productId,
        CancellationToken ct)
    {
        var inventory = await service.GetByProductIdAsync(productId, ct);
        return inventory is null ? NotFound() : Ok(inventory);
    }

    [HttpPut("product/{productId:long}")]
    public async Task<ActionResult<InventoryResponseDto>> Update(
        long productId,
        UpdateInventoryDto dto,
        CancellationToken ct)
    {
        var inventory = await service.UpdateAsync(productId, dto, ct);
        return inventory is null ? NotFound() : Ok(inventory);
    }
}
