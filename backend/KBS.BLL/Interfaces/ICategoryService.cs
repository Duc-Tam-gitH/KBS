using KBS.Models.DTOs.Categories;

namespace KBS.BLL.Interfaces;

public interface ICategoryService
{
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<List<CategoryResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CategoryResponseDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<CategoryResponseDto> CreateAsync(
        CategoryCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<CategoryResponseDto?> UpdateAsync(
        long id,
        CategoryUpdateDto dto,
        CancellationToken cancellationToken = default);
}
