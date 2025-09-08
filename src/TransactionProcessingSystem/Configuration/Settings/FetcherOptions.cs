using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Settings;

public record FetcherOptions
{
    public const string SectionName = "Fetcher";

    [Required]
    public required string InputDirectory { get; init; }

    [Required]
    public required string Encoding { get; init; }
}
