using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Configuration settings for Neo4j database connection and behavior.
/// Secret values (URI, username, password) are bound separately via SecretsSettings.
/// </summary>
public record Neo4jSettings
{
    /// <summary>
    /// Target database name
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Database { get; init; } = "neo4j";

    /// <summary>
    /// Maximum number of connections in the pool
    /// </summary>
    [Range(1, 1000)]
    public int MaxConnectionPoolSize { get; init; } = 50;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int ConnectionTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum retry time for transactions in seconds
    /// </summary>
    [Range(1, 300)]
    public int MaxTransactionRetryTimeSeconds { get; init; } = 30;
}
