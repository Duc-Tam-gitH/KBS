using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Inventory;
using KBS.Models.DTOs.Staff;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class InventoryService(KbsDbContext db, StaffService staff) : IInventoryService
{
    public async Task<InventoryResponseDto?> GetByProductIdAsync(long productId, CancellationToken ct = default)
    {
        var inventory = await db.Inventories.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        return inventory is null ? null : new InventoryResponseDto
        {
            Id = inventory.Id, ProductId = inventory.ProductId,
            StockQuantity = inventory.StockQuantity, UpdatedAt = inventory.UpdatedAt
        };
    }

    public async Task<InventoryResponseDto?> UpdateAsync(
        long productId,
        UpdateInventoryDto dto,
        long actorId,
        CancellationToken ct = default)
    {
        var inventory = await GetByProductIdAsync(productId, ct);
        if (inventory is null) return null;
        if (string.IsNullOrWhiteSpace(dto.Reason)) throw new ArgumentException("Cần lý do điều chỉnh tồn kho.");
        if (dto.StockQuantity == inventory.StockQuantity) return inventory;
        await staff.AdjustStockAsync(productId, actorId, new StockAdjustmentRequest
        {
            Delta = dto.StockQuantity - inventory.StockQuantity,
            ExpectedQuantity = inventory.StockQuantity,
            Reason = dto.Reason
        }, ct);
        return await GetByProductIdAsync(productId, ct);
    }
}
