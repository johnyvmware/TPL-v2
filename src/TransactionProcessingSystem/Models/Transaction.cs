namespace TransactionProcessingSystem.Models;

public record Transaction
{
    public required string Id { get; init; }
    public required DateTime Date { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public string? CleanDescription { get; init; }
    public string? EmailSubject { get; init; }
    public string? EmailSnippet { get; init; }
    public string? Category { get; init; }
    public ProcessingStatus Status { get; init; } = ProcessingStatus.Fetched;
}

public enum ProcessingStatus
{
    Fetched,
    Processed,
    EmailEnriched,
    Categorized,
    Exported,
    Failed,
    Cancelled
}

public record TransactionApiResponse
{
    public required IEnumerable<RawTransaction> Transactions { get; init; }
}

public record RawTransaction
{
    public required string Id { get; init; }
    public required string Date { get; init; }
    public required string Amount { get; init; }
    public required string Description { get; init; }
}

public record EmailMatch
{
    public required string Subject { get; init; }
    public required string Snippet { get; init; }
    public required DateTime ReceivedDateTime { get; init; }
}