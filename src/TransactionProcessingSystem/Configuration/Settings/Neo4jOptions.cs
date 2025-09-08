using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Settings;

public record Neo4jOptions
{
    public const string SectionName = "Neo4j";

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Database { get; init; } = "neo4j";

    [Required]
    [Range(1, 10)]
    public int MaxConnectionPoolSize { get; init; }

    [Required]
    [Range(1, 60)]
    public int ConnectionTimeoutSeconds { get; init; }

    [Required]
    [Range(1, 15)]
    public int MaxTransactionRetryTimeSeconds { get; init; }
}
