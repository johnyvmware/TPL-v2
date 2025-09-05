namespace TransactionProcessingSystem.Models;

public record CategoryAssignment
{
    public required string Reasoning { get; init; }

    public required string MainCategory { get; init; }

    public required string SubCategory { get; init; }
}
