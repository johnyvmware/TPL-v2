using CsvHelper.Configuration.Attributes;

namespace TransactionProcessingSystem.Models;

[Delimiter(";")]
[CultureInfo("pl-PL")]
public record RawTransaction
{
    [Index(0)]
    public DateTime Date { get; init; }

    [Index(2)]
    public string? Description { get; init; }

    [Index(3)]
    public string? Title { get; init; }

    [Index(6)]
    public decimal Amount { get; init; }
}
