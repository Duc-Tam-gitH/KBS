using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Common;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace KBS.BLL.Services;
public class ProductService(KbsDbContext db) : IProductService
{
    public async Task<PagedResponse<ProductResponseDto>> GetAllAsync(string? search, long? categoryId, long? brandId, decimal? minPrice, decimal? maxPrice, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLower(); q = q.Where(p => p.Name.ToLower().Contains(term)); }
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (brandId.HasValue) q = q.Where(p => p.BrandId == brandId);
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice);
        var total = await q.CountAsync(ct);
        var products = await q.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResponse<ProductResponseDto> { Items = products.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }
    public async Task<ProductResponseDto?> GetByIdAsync(long id, CancellationToken ct = default) { var product = await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct); return product is null ? null : ToDto(product); }
    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto, CancellationToken ct = default)
    { await ValidateReferences(dto.Name, dto.Price, dto.OriginalPrice, dto.CategoryId, dto.BrandId, ct); var p = new Product { Name = dto.Name.Trim(), Price = dto.Price, OriginalPrice = dto.OriginalPrice, Description = dto.Description?.Trim(), CategoryId = dto.CategoryId, BrandId = dto.BrandId, SwitchType = dto.SwitchType?.Trim(), Layout = dto.Layout?.Trim(), ConnectionType = dto.ConnectionType?.Trim(), Keycap = dto.Keycap?.Trim(), Color = dto.Color?.Trim(), IsHotSwap = dto.IsHotSwap, IsRGB = dto.IsRGB }; ApplyImages(p, dto.Images); db.Products.Add(p); db.Inventories.Add(new Inventory { Product = p, StockQuantity = 0 }); await db.SaveChangesAsync(ct); return ToDto(p); }
    public async Task<ProductResponseDto?> UpdateAsync(long id, ProductUpdateDto dto, CancellationToken ct = default)
    { var p = await db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct); if (p is null) return null; await ValidateReferences(dto.Name, dto.Price, dto.OriginalPrice, dto.CategoryId, dto.BrandId, ct); p.Name=dto.Name.Trim(); p.Price=dto.Price; p.OriginalPrice=dto.OriginalPrice; p.Description=dto.Description?.Trim(); p.CategoryId=dto.CategoryId; p.BrandId=dto.BrandId; p.SwitchType=dto.SwitchType?.Trim(); p.Layout=dto.Layout?.Trim(); p.ConnectionType=dto.ConnectionType?.Trim(); p.Keycap=dto.Keycap?.Trim(); p.Color=dto.Color?.Trim(); p.IsHotSwap=dto.IsHotSwap; p.IsRGB=dto.IsRGB; p.IsActive=dto.IsActive; db.ProductImages.RemoveRange(p.Images); p.Images.Clear(); ApplyImages(p, dto.Images); await db.SaveChangesAsync(ct); return ToDto(p); }
    private async Task ValidateReferences(string name, decimal price, decimal? originalPrice, long categoryId, long brandId, CancellationToken ct) { if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên sản phẩm không được để trống."); if (price < 0 || originalPrice < 0) throw new ArgumentException("Giá sản phẩm không hợp lệ."); if (!await db.Categories.AnyAsync(x => x.Id == categoryId, ct)) throw new InvalidOperationException("Danh mục không tồn tại."); if (!await db.Brands.AnyAsync(x => x.Id == brandId && x.IsActive, ct)) throw new InvalidOperationException("Thương hiệu không tồn tại hoặc đã ngừng hoạt động."); }
    private static void ApplyImages(Product product, IEnumerable<ProductImageDto> images) { foreach (var image in images.OrderBy(x => x.SortOrder)) product.Images.Add(new ProductImage { ImageUrl=image.ImageUrl.Trim(), AltText=image.AltText?.Trim(), SortOrder=image.SortOrder }); }
    private static ProductResponseDto ToDto(Product p) => new() { Id=p.Id, Name=p.Name, Price=p.Price, OriginalPrice=p.OriginalPrice, Description=p.Description, CategoryId=p.CategoryId, CategoryName=p.Category?.Name ?? string.Empty, BrandId=p.BrandId, BrandName=p.Brand?.Name ?? string.Empty, SwitchType=p.SwitchType, Layout=p.Layout, ConnectionType=p.ConnectionType, Keycap=p.Keycap, Color=p.Color, IsHotSwap=p.IsHotSwap, IsRGB=p.IsRGB, IsActive=p.IsActive, CreatedAt=p.CreatedAt, Images=p.Images.OrderBy(i=>i.SortOrder).Select(i=>new ProductImageDto{Id=i.Id,ImageUrl=i.ImageUrl,AltText=i.AltText,SortOrder=i.SortOrder}).ToList() };
}
