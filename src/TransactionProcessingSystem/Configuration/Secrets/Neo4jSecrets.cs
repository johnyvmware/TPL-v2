using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Secrets;

public record Neo4jSecrets
{
    public const string SectionName = "Neo4j";

    [Required]
    public required string Uri { get; init; }

    [Required]
    public required string User { get; init; }

    [Required]
    public required string Password { get; init; }
}
