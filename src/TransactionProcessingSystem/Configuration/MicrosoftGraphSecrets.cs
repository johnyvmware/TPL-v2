using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Microsoft Graph secret configuration
/// </summary>
public record MicrosoftGraphSecrets
{
    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }

    [Required]
    public required string TenantId { get; init; }
}
