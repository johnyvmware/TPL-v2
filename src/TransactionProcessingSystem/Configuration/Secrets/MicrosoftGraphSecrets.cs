using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Secrets;

public record MicrosoftGraphSecrets
{
    public const string SectionName = "MicrosoftGraph";

    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string TenantId { get; init; }
}
