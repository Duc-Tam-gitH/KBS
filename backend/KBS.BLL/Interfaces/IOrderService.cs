using KBS.Models.DTOs.Orders;
namespace KBS.BLL.Interfaces;
public interface IOrderService { Task<OrderResponseDto> CreateAsync(long userId, CreateOrderDto dto, CancellationToken ct = default); Task<List<OrderResponseDto>> GetForUserAsync(long userId, CancellationToken ct = default); Task<List<OrderResponseDto>> GetAllAsync(CancellationToken ct = default); Task<OrderResponseDto?> UpdateStatusAsync(long id, UpdateOrderStatusDto dto, CancellationToken ct = default); }
