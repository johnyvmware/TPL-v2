using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

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