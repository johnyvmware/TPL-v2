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

/// <summary>
/// Neo4j secret configuration
/// </summary>
public record Neo4jSecrets
{
    [Required]
    public required string Uri { get; init; }

    [Required]
    public required string User { get; init; }

    [Required]
    public required string Password { get; init; }
}