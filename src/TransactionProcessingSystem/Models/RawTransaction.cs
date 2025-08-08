using CsvHelper.Configuration.Attributes;

namespace TransactionProcessingSystem.Models;

[Delimiter(";")]
[CultureInfo("pl-PL")]
public record RawTransaction
{
    [Index(0)]
    public required DateTime Date { get; init; }

    [Index(2)]
    public required string Description { get; init; }

    [Index(3)]
    public required string Title { get; init; }

    [Index(4)]
    public required string Receiver { get; init; }

    [Index(6)]
    public required decimal Amount { get; init; }
}
