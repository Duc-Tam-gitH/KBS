using KBS.Models.DTOs.Inventory;
namespace KBS.BLL.Interfaces;
public interface IInventoryService { Task<InventoryResponseDto?> GetByProductIdAsync(long productId, CancellationToken ct = default); Task<InventoryResponseDto?> UpdateAsync(long productId, UpdateInventoryDto dto, CancellationToken ct = default); }
