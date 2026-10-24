namespace KBS.Models.DTOs.Staff;

public class StaffActivityDto
{
    public long Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class StaffSummaryDto
{
    public int NewOrdersCount { get; set; }
    public int ProcessingOrdersCount { get; set; }
    public int LowStockCount { get; set; }
    public int UnreadMessagesCount { get; set; }
    public int UnassignedConversations { get; set; }
    public List<StaffActivityDto> RecentMovements { get; set; } = new();
}

public class StaffInventoryDto
{
    public long ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class InventoryMovementDto
{
    public long Id { get; set; }
    public long ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public int Delta { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class StaffCustomerDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
}

public class StaffReviewDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class OrderNoteDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string StaffName { get; set; } = string.Empty;
}

public class OrderOperationsDto
{
    public string? TrackingNumber { get; set; }
    public List<OrderNoteDto> Notes { get; set; } = new();
}

public class ChatConversationDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public long? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? LastMessage { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int Unread { get; set; }
}

public class ChatMessageDto
{
    public long Id { get; set; }
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
