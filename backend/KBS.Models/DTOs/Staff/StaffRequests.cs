using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Staff;

public record TextRequest
{
    [Required]
    [StringLength(2000)]
    public string Content { get; init; } = string.Empty;
}

public record StockAdjustmentRequest
{
    public int Delta { get; init; }
    [Range(0, int.MaxValue)]
    public int ExpectedQuantity { get; init; }
    [StringLength(500)]
    public string? Reason { get; init; }
}

public record ThresholdRequest
{
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; init; }
}

public record ChatSendRequest
{
    [Required]
    [StringLength(4000)]
    public string Content { get; init; } = string.Empty;
}

public record ChatReadRequest
{
    [Range(1, long.MaxValue)]
    public long ThroughMessageId { get; init; }
}
