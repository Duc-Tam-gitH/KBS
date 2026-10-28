using KBS.BLL.Interfaces;
using KBS.DAL.Data;
using KBS.Models.DTOs.Categories;
using KBS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KBS.BLL.Services;

public class CategoryService(KbsDbContext dbContext) : ICategoryService
{
    public async Task<List<CategoryResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                ProductCount = c.Products.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                ProductCount = c.Products.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryResponseDto> CreateAsync(
        CategoryCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên danh mục không được để trống.");
        }

        var nameExists = await dbContext.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException("Tên danh mục đã tồn tại.");
        }

        var category = new Category
        {
            Name = name,
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryResponseDto?> UpdateAsync(
        long id,
        CategoryUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            return null;
        }

        var name = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tên danh mục không được để trống.");
        }

        var nameExists = await dbContext.Categories
            .AnyAsync(c =>
                    c.Id != id &&
                    c.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException("Tên danh mục đã tồn tại.");
        }

        category.Name = name;
        category.Description = dto.Description?.Trim();
        category.IsActive = dto.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            ProductCount = await dbContext.Products.CountAsync(p => p.CategoryId == id, cancellationToken)
        };
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null) return false;
        if (await dbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            throw new InvalidOperationException("Danh mục đang có sản phẩm. Hãy chuyển sản phẩm trước khi xóa.");
        }

        dbContext.Categories.Remove(category);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23503" })
        {
            throw new InvalidOperationException("Danh mục đang có sản phẩm, không thể xóa.", error);
        }
        return true;
    }
}
