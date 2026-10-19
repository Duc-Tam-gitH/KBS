using KBS.Models.DTOs.Brands;
namespace KBS.BLL.Interfaces;
public interface IBrandService { Task<List<BrandResponseDto>> GetAllAsync(CancellationToken ct = default); Task<BrandResponseDto?> GetByIdAsync(long id, CancellationToken ct = default); Task<BrandResponseDto> CreateAsync(BrandCreateDto dto, CancellationToken ct = default); Task<BrandResponseDto?> UpdateAsync(long id, BrandUpdateDto dto, CancellationToken ct = default); }
