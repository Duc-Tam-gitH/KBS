using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Ai;

public record AiModuleResponseDto(
    string Id,
    string Name,
    string Description,
    string SourceFile,
    bool IsIntegrated,
    bool IsEnabled,
    decimal? ApprovalLimit,
    DateTimeOffset? LastHeartbeatAt,
    DateTimeOffset UpdatedAt,
    string ConnectionStatus);

public record AiEventResponseDto(
    long Id,
    string ModuleId,
    string ModuleName,
    long? OrderId,
    string Kind,
    string Message,
    DateTimeOffset CreatedAt);

public record AiEventsPage(List<AiEventResponseDto> Items, long? NextBeforeId);
public record AiOrderDto(long Id, decimal TotalPrice);
public record AiPollDto(bool IsEnabled, decimal ApprovalLimit, List<AiOrderDto> Orders);
public record AiDecisionResponseDto(long OrderId, string Result);
public class AiEnabledRequest
{
    [Required]
    public bool? IsEnabled { get; set; }
}
public class AiDecisionRequest
{
    [Required]
    [RegularExpression("^(CONFIRM|MANUAL_REVIEW)$")]
    public string Decision { get; set; } = string.Empty;
}
