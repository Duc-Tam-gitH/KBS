using System.ComponentModel.DataAnnotations;
using KBS.DAL.Data;
using KBS.Models.DTOs.Shop;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class ShopSettingsService(KbsDbContext db)
{
    public async Task<PublicShopDto> PublicAsync(CancellationToken ct) =>
        await db.ShopSettings.AsNoTracking().Select(x => new PublicShopDto(
            x.Name, x.Slogan, x.Description, x.Hotline, x.Email, x.WarehouseAddress,
            x.LogoData != null ? "/api/shop/media/logo" : null,
            x.BannerData != null ? "/api/shop/media/banner" : null, x.UpdatedAt)).SingleOrDefaultAsync(ct)
        ?? new PublicShopDto(null, null, null, null, null, null, null, null, null);

    public async Task<AdminShopDto> AdminAsync(CancellationToken ct)
    {
        var shop = await PublicAsync(ct);
        var bank = await db.ShopSettings.AsNoTracking().Select(x => new
        {
            x.BankName, x.BankBin, x.AccountNumber, x.AccountName
        }).SingleOrDefaultAsync(ct);
        return new AdminShopDto(shop, bank?.BankName, bank?.BankBin, bank?.AccountNumber, bank?.AccountName);
    }

    public async Task<AdminShopDto> SaveAsync(SaveShopSettingsDto dto, CancellationToken ct)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
            throw new ArgumentException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        var entity = await EditableAsync(ct);
        entity.Name = Clean(dto.Name);
        entity.Slogan = Clean(dto.Slogan);
        entity.Description = Clean(dto.Description);
        entity.Hotline = Clean(dto.Hotline);
        entity.Email = Clean(dto.Email);
        entity.WarehouseAddress = Clean(dto.WarehouseAddress);
        entity.BankName = Clean(dto.BankName);
        entity.BankBin = Clean(dto.BankBin);
        entity.AccountNumber = Clean(dto.AccountNumber);
        entity.AccountName = Clean(dto.AccountName);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await AdminAsync(ct);
    }

    public async Task SaveMediaAsync(
        string kind,
        byte[] data,
        string contentType,
        CancellationToken ct)
    {
        var limit = MediaLimit(kind);
        if (data.Length == 0 || data.Length > limit) throw new ArgumentException("Ảnh vượt dung lượng cho phép.");
        var png = data.Length >= 45 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && data.AsSpan(12, 4).SequenceEqual("IHDR"u8) && data.AsSpan(data.Length - 8, 4).SequenceEqual("IEND"u8);
        var jpeg = data.Length >= 4 && data[0] == 255 && data[1] == 216
            && data[2] == 255 && data[^2] == 255 && data[^1] == 217;
        if (!(contentType == "image/png" && png) && !(contentType == "image/jpeg" && jpeg))
            throw new ArgumentException("Chỉ nhận ảnh PNG hoặc JPEG có nội dung phù hợp định dạng.");
        var entity = await EditableAsync(ct);
        if (kind == "logo") { entity.LogoData = data; entity.LogoContentType = contentType; }
        else { entity.BannerData = data; entity.BannerContentType = contentType; }
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<(byte[] Data, string ContentType)?> MediaAsync(string kind, CancellationToken ct)
    {
        MediaLimit(kind);
        var media = kind == "logo"
            ? await db.ShopSettings.AsNoTracking().Select(x => new { Data = x.LogoData, Type = x.LogoContentType })
                .SingleOrDefaultAsync(ct)
            : await db.ShopSettings.AsNoTracking().Select(x => new { Data = x.BannerData, Type = x.BannerContentType })
                .SingleOrDefaultAsync(ct);
        return media?.Data == null ? null : (media.Data, media.Type!);
    }

    public async Task DeleteMediaAsync(string kind, CancellationToken ct)
    {
        MediaLimit(kind);
        var entity = await db.ShopSettings.SingleOrDefaultAsync(ct);
        if (entity == null) return;
        if (kind == "logo") { entity.LogoData = null; entity.LogoContentType = null; }
        else { entity.BannerData = null; entity.BannerContentType = null; }
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public static int MediaLimit(string kind) => kind switch
    {
        "logo" => 2 * 1024 * 1024,
        "banner" => 5 * 1024 * 1024,
        _ => throw new ArgumentException("Loại ảnh không hợp lệ.")
    };

    private async Task<ShopSettings> EditableAsync(CancellationToken ct)
    {
        var entity = await db.ShopSettings.SingleOrDefaultAsync(ct);
        if (entity != null) return entity;
        entity = new ShopSettings();
        db.ShopSettings.Add(entity);
        return entity;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
