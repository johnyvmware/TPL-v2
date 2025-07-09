using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Configuration settings for Neo4j database connection and behavior.
/// Supports binding from appsettings.json and user secrets.
/// </summary>
public record Neo4jSettings
{
    /// <summary>
    /// The Neo4j connection URI (e.g., neo4j+s://your-instance.databases.neo4j.io)
    /// </summary>
    [Required]
    [Url]
    public required string ConnectionUri { get; init; }

    /// <summary>
    /// Username for Neo4j authentication
    /// </summary>
    [Required]
    public required string Username { get; init; }

    /// <summary>
    /// Password for Neo4j authentication
    /// </summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>
    /// Target database name
    /// </summary>
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

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(ConnectionUri) &&
                          !string.IsNullOrWhiteSpace(Username) &&
                          !string.IsNullOrWhiteSpace(Password);
}
