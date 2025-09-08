namespace TransactionProcessingSystem.Services.Categorizer;

public record CategoryInfo
{
    public required string Name { get; init; }

    public required string Definition { get; init; }
}
