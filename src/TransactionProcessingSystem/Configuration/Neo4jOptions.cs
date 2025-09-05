using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Configuration settings for Neo4j database connection and behavior.
/// </summary>
public record Neo4jOptions
{
    /// <summary>
    /// Target database name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Database { get; init; } = "neo4j";

    /// <summary>
    /// Maximum number of connections in the pool.
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int MaxConnectionPoolSize { get; init; }

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Required]
    [Range(1, 60)]
    public int ConnectionTimeoutSeconds { get; init; }

    /// <summary>
    /// Maximum retry time for transactions in seconds.
    /// </summary>
    [Required]
    [Range(1, 15)]
    public int MaxTransactionRetryTimeSeconds { get; init; }
}
