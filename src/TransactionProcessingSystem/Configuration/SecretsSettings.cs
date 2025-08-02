using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Secret configuration values bound from User Secrets (development) or Environment Variables (production).
/// These values should never be stored in appsettings.json files.
/// </summary>
public record SecretsSettings
{
    public required OpenAISecrets OpenAI { get; init; }
    public required MicrosoftGraphSecrets MicrosoftGraph { get; init; }
    public required Neo4jSecrets Neo4j { get; init; }
}

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