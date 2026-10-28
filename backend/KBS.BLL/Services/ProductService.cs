using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Common;
using KBS.Models.DTOs.Products;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class ProductService(KbsDbContext db) : IProductService
{
    public async Task<List<ProductResponseDto>> GetForManagementAsync(CancellationToken ct = default)
    {
        var products = await Query().OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).ToListAsync(ct);
        return products.Select(ToDto).ToList();
    }

    public async Task<PagedResponse<ProductResponseDto>> GetAllAsync(
        string? search,
        long? categoryId,
        long? brandId,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = Query().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId);
        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);
        var total = await query.CountAsync(ct);
        var products = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResponse<ProductResponseDto>
        {
            Items = products.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<ProductResponseDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var product = await Query().FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);
        return product is null ? null : ToDto(product);
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto, CancellationToken ct = default)
    {
        await ValidateReferences(dto.Name, dto.Price, dto.OriginalPrice, dto.CategoryId, dto.BrandId, ct);
        var product = new Product
        {
            Name = dto.Name.Trim(),
            Price = dto.Price,
            OriginalPrice = dto.OriginalPrice,
            Description = dto.Description?.Trim(),
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            SwitchType = dto.SwitchType?.Trim(),
            Layout = dto.Layout?.Trim(),
            ConnectionType = dto.ConnectionType?.Trim(),
            Keycap = dto.Keycap?.Trim(),
            Color = dto.Color?.Trim(),
            IsHotSwap = dto.IsHotSwap,
            IsRGB = dto.IsRGB
        };
        ApplyImages(product, dto.Images);
        db.Products.Add(product);
        db.Inventories.Add(new Inventory { Product = product, StockQuantity = 0 });
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(product.Id, ct))!;
    }

    public async Task<ProductResponseDto?> UpdateAsync(
        long id,
        ProductUpdateDto dto,
        CancellationToken ct = default)
    {
        var product = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return null;
        await ValidateReferences(dto.Name, dto.Price, dto.OriginalPrice, dto.CategoryId, dto.BrandId, ct);
        product.Name = dto.Name.Trim();
        product.Price = dto.Price;
        product.OriginalPrice = dto.OriginalPrice;
        product.Description = dto.Description?.Trim();
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.SwitchType = dto.SwitchType?.Trim();
        product.Layout = dto.Layout?.Trim();
        product.ConnectionType = dto.ConnectionType?.Trim();
        product.Keycap = dto.Keycap?.Trim();
        product.Color = dto.Color?.Trim();
        product.IsHotSwap = dto.IsHotSwap;
        product.IsRGB = dto.IsRGB;
        product.IsActive = dto.IsActive;
        db.ProductImages.RemoveRange(product.Images);
        product.Images.Clear();
        ApplyImages(product, dto.Images);
        await db.SaveChangesAsync(ct);
        return ToDto(await Query().SingleAsync(p => p.Id == id, ct));
    }

    private IQueryable<Product> Query() => db.Products.AsNoTracking()
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .Include(p => p.Images)
        .Include(p => p.Inventory);

    private async Task ValidateReferences(
        string name,
        decimal price,
        decimal? originalPrice,
        long categoryId,
        long brandId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên sản phẩm không được để trống.");
        if (price < 0 || originalPrice < 0)
            throw new ArgumentException("Giá sản phẩm không hợp lệ.");
        if (!await db.Categories.AnyAsync(x => x.Id == categoryId, ct))
            throw new InvalidOperationException("Danh mục không tồn tại.");
        if (!await db.Brands.AnyAsync(x => x.Id == brandId && x.IsActive, ct))
            throw new InvalidOperationException("Thương hiệu không tồn tại hoặc đã ngừng hoạt động.");
    }

    private static void ApplyImages(Product product, IEnumerable<ProductImageDto> images)
    {
        foreach (var image in images.OrderBy(x => x.SortOrder))
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = image.ImageUrl.Trim(),
                AltText = image.AltText?.Trim(),
                SortOrder = image.SortOrder
            });
        }
    }

    private static ProductResponseDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        OriginalPrice = product.OriginalPrice,
        Description = product.Description,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        BrandId = product.BrandId,
        BrandName = product.Brand?.Name ?? string.Empty,
        SwitchType = product.SwitchType,
        Layout = product.Layout,
        ConnectionType = product.ConnectionType,
        Keycap = product.Keycap,
        Color = product.Color,
        IsHotSwap = product.IsHotSwap,
        IsRGB = product.IsRGB,
        StockQuantity = product.Inventory?.StockQuantity ?? 0,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        Images = product.Images.OrderBy(image => image.SortOrder).Select(image => new ProductImageDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            AltText = image.AltText,
            SortOrder = image.SortOrder
        }).ToList()
    };
}
