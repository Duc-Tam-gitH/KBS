namespace KBS.Models.Entities;

public class StaffShift
{
    public long Id { get; set; }
    public long StaffId { get; set; }
    public User? Staff { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
}
