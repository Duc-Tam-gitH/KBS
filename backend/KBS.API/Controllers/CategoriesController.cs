using KBS.BLL.Interfaces;
using KBS.Models.DTOs.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KBS.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize] 
public class CategoriesController(ICategoryService categoryService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return NotFound(new { message = "Không tìm thấy danh mục." });
        }

        return Ok(category);
    }
    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create(
        [FromBody] CategoryCreateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryService.CreateAsync(dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = category.Id },
                category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(
        long id,
        [FromBody] CategoryUpdateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryService.UpdateAsync(
                id, dto, cancellationToken);

            if (category is null)
            {
                return NotFound(new { message = "Không tìm thấy danh mục." });
            }

            return Ok(category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
