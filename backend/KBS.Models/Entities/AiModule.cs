namespace KBS.Models.Entities;

public class AiModule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public bool IsIntegrated { get; set; }
    public bool IsEnabled { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
