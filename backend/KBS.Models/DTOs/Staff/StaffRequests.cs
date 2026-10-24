using System.ComponentModel.DataAnnotations;

namespace KBS.Models.DTOs.Staff;

public class TextRequest
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class TrackingRequest
{
    [StringLength(100)]
    public string? TrackingNumber { get; set; }
}

public class StockAdjustmentRequest
{
    public int Delta { get; set; }
    [Range(0, int.MaxValue)]
    public int ExpectedQuantity { get; set; }
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ThresholdRequest
{
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; }
}

public class ChatSendRequest
{
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatReadRequest
{
    [Range(1, long.MaxValue)]
    public long ThroughMessageId { get; set; }
}
