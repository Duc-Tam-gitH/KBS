using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[TypeFilter(typeof(StaffOperationFilter))]
public class OrdersController(IOrderService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> Create(
        CreateOrderDto dto,
        CancellationToken ct)
    {
        try
        {
            var order = await service.CreateAsync(UserId(), dto, ct);
            return CreatedAtAction(nameof(MyOrders), order);
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

    [HttpGet("mine")]
    public Task<List<OrderResponseDto>> MyOrders(CancellationToken ct) =>
        service.GetForUserAsync(UserId(), ct);

    [HttpPut("{id:long}/receipt")]
    public async Task<ActionResult<OrderResponseDto>> ConfirmReceipt(
        [Range(1, long.MaxValue)] long id,
        CancellationToken ct)
    {
        try
        {
            var order = await service.ConfirmReceiptAsync(UserId(), id, ct);
            return order is null ? NotFound() : Ok(order);
        }
        catch (InvalidOperationException error)
        {
            return Conflict(new { message = error.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public Task<List<OrderResponseDto>> GetAll(CancellationToken ct) => service.GetAllAsync(ct);

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id:long}/status")]
    public async Task<ActionResult<OrderResponseDto>> Status(
        long id,
        UpdateOrderStatusDto dto,
        CancellationToken ct)
    {
        try
        {
            var order = await service.UpdateStatusAsync(id, dto, UserId(), ct);
            return order is null ? NotFound() : Ok(order);
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

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
