using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Microsoft Graph configuration.
/// </summary>
public record MicrosoftGraphOptions
{
    [Required]
    [Range(1, 365)]
    public required int EmailSearchDays { get; init; }
}
