using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Settings;

public record MicrosoftGraphOptions
{
    public const string SectionName = "MicrosoftGraph";

    [Required]
    public required string[] Scopes { get; init; }

    [Required]
    public required string CacheFileName { get; init; }

    [Required]
    public required string CacheDirectory { get; init; }
}
