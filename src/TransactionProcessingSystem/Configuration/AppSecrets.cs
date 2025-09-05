using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// OpenAI secret configuration
/// </summary>
public record OpenAISecrets
{
    [Required]
    public required string ApiKey { get; init; }
}
