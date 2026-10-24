using System.ComponentModel.DataAnnotations;
using KBS.Models.DTOs.Common;

namespace KBS.Models.DTOs.Products;

public class ProductReviewCreateDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}

public class ProductReviewResponseDto
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class ProductReviewsResponseDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingCounts { get; set; } = new();
    public PagedResponse<ProductReviewResponseDto> Reviews { get; set; } = new();
}
