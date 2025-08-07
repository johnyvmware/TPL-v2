namespace TransactionProcessingSystem.Models;

public record Transaction
{
    public required DateTime Date { get; init; }

    public required string Description { get; init; }

    public required decimal Amount { get; init; }
}