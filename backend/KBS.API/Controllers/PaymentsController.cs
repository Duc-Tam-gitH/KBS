using System.Security.Claims;
using KBS.BLL.Services;
using KBS.Models.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
[TypeFilter(typeof(StaffOperationFilter))]
public class PaymentsController(PaymentService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) => Ok(new
    {
        bankAvailable = await service.BankAvailableAsync(ct)
    });

    [HttpGet("orders/{id:long}")]
    public Task<PaymentResponseDto> Get(long id, CancellationToken ct) => service.GetAsync(UserId(), id, ct);

    [HttpPost("orders/{id:long}")]
    public Task<PaymentResponseDto> Create(long id, CancellationToken ct)
        => service.CreateAsync(UserId(), id, ct);

    [Authorize(Roles = "Admin,Staff,Nhân viên")]
    [HttpPut("orders/{id:long}/bank-confirmation")]
    public Task<PaymentResponseDto> ConfirmBank(long id, CancellationToken ct)
        => service.ConfirmBankAsync(UserId(), id, ct);

    private long UserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
