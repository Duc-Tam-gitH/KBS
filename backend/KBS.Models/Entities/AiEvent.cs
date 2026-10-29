namespace KBS.Models.Entities;

public class AiEvent
{
    public long Id { get; set; }
    public string ModuleId { get; set; } = string.Empty;
    public AiModule Module { get; set; } = null!;
    public long? OrderId { get; set; }
    public long? ActorId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
