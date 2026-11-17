namespace KBS.Models.DTOs.Payments;

public record BankTransactionDto(
    string Id,
    string BankAccountId,
    string AccountNumber,
    decimal Amount,
    string Content,
    DateTimeOffset OccurredAt);
