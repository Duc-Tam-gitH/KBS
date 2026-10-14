namespace KBS.Models.DTOs.Brands;

public class BrandCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
