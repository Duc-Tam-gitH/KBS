using System.Security.Claims;
using KBS.BLL.Interfaces;
using KBS.BLL.Services;
using KBS.Models.DTOs.Staff;
using KBS.Models.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Admin,Staff")]
[TypeFilter(typeof(StaffOperationFilter))]
public class StaffController(StaffService service, IOrderService orders) : ControllerBase
{
    [HttpGet("summary")]
    public Task<StaffSummaryDto> Summary(CancellationToken ct) => service.SummaryAsync(UserId(), ct);

    [HttpGet("inventory")]
    public Task<List<StaffInventoryDto>> Inventory(CancellationToken ct) => service.InventoryAsync(ct);

    [HttpGet("inventory/{id:long}/movements")]
    public Task<List<InventoryMovementDto>> Movements(long id, CancellationToken ct) => service.MovementsAsync(id, ct);

    [HttpPost("inventory/{id:long}/adjustments")]
    public async Task<IActionResult> Adjust(
        long id,
        StockAdjustmentRequest dto,
        CancellationToken ct)
    {
        await service.AdjustStockAsync(id, UserId(), dto, ct);
        return NoContent();
    }

    [HttpPut("inventory/{id:long}/threshold")]
    public async Task<IActionResult> Threshold(
        long id,
        ThresholdRequest dto,
        CancellationToken ct)
    {
        await service.ThresholdAsync(id, dto.LowStockThreshold, ct);
        return NoContent();
    }

    [HttpGet("customers")]
    public Task<List<StaffCustomerDto>> Customers(CancellationToken ct) => service.CustomersAsync(ct);

    [HttpGet("customers/{id:long}/orders")]
    public async Task<IActionResult> CustomerOrders(long id, CancellationToken ct)
    {
        await service.EnsureCustomerAsync(id, ct);
        return Ok(await orders.GetForUserAsync(id, ct));
    }

    [HttpGet("customers/{id:long}/reviews")]
    public Task<List<StaffReviewDto>> CustomerReviews(long id, CancellationToken ct) =>
        service.CustomerReviewsAsync(id, ct);

    [HttpPut("customers/{id:long}/status")]
    public async Task<IActionResult> CustomerStatus(
        long id,
        UpdateUserStatusDto dto,
        CancellationToken ct)
    {
        await service.SetCustomerStatusAsync(id, dto.IsActive!.Value, ct);
        return NoContent();
    }

    [HttpGet("orders/{id:long}/operations")]
    public Task<OrderOperationsDto> OrderOperations(long id, CancellationToken ct) =>
        service.OrderOperationsAsync(id, ct);

    [HttpPut("orders/{id:long}/tracking")]
    public async Task<IActionResult> Tracking(long id, CancellationToken ct)
    {
        await service.TrackingAsync(id, ct);
        return NoContent();
    }

    [HttpPost("orders/{id:long}/notes")]
    public async Task<IActionResult> AddNote(
        long id,
        TextRequest dto,
        CancellationToken ct)
    {
        await service.AddNoteAsync(id, UserId(), dto.Content, ct);
        return NoContent();
    }

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
