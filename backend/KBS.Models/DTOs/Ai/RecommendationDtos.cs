using System.ComponentModel.DataAnnotations;
using KBS.Models.DTOs.Products;

namespace KBS.Models.DTOs.Ai;

public record RecommendationProduct(
    long Id, long CategoryId, long BrandId, decimal Price, string? Sound, string? SwitchType, string? Layout);
public record RecommendationHistory(long UserId, List<RecommendationProduct> Products);
public record RecommendationPoll(
    bool IsEnabled, DateTimeOffset Version, List<RecommendationProduct> Products,
    List<RecommendationHistory> Histories);
public class CustomerRecommendations
{
    public long UserId { get; set; }
    [Required]
    [MaxLength(8)]
    public long[] ProductIds { get; set; } = [];
}
public class RecommendationUpdate
{
    public DateTimeOffset Version { get; set; }
    [Required]
    [MaxLength(8)]
    public long[] Random { get; set; } = [];
    [Required]
    public List<CustomerRecommendations> Customers { get; set; } = [];
}
public record RecommendationPage(string Status, List<ProductResponseDto> Items, string Mode = "RANDOM");
