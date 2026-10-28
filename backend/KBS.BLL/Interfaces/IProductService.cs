using KBS.Models.DTOs.Common;
using KBS.Models.DTOs.Products;
namespace KBS.BLL.Interfaces;
public interface IProductService
{
    Task<List<ProductResponseDto>> GetForManagementAsync(CancellationToken ct = default);
    Task<PagedResponse<ProductResponseDto>> GetAllAsync(string? search, long? categoryId, long? brandId, decimal? minPrice, decimal? maxPrice, int page, int pageSize, CancellationToken ct = default);
    Task<ProductResponseDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProductResponseDto> CreateAsync(ProductCreateDto dto, CancellationToken ct = default);
    Task<ProductResponseDto?> UpdateAsync(long id, ProductUpdateDto dto, CancellationToken ct = default);
}
