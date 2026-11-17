using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Ai;

public record PendingReview(long Id, string Comment);
public record ReviewPoll(bool IsEnabled, List<PendingReview> Reviews);
public record ReviewDecisionResult(long Id, string Status);
public class ReviewWorkerDecision
{
    [Required]
    [RegularExpression("^(APPROVED|FLAGGED)$")]
    public string Decision { get; set; } = string.Empty;
    [Required]
    [MaxLength(50)]
    public string[] MatchedKeywords { get; set; } = [];
}
public class ReviewStaffDecision
{
    [Required]
    [RegularExpression("^(APPROVED|HIDDEN)$")]
    public string Status { get; set; } = string.Empty;
}
public record ModerationReview(
    long Id, long ProductId, string ProductName, string DisplayName, int Rating, string Comment,
    string Status, string? Reason, DateTimeOffset CreatedAt, DateTimeOffset? CheckedAt);
