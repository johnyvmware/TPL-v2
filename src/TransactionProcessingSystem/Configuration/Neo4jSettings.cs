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

/// <summary>
/// Combined Neo4j configuration that includes both settings and secrets
/// </summary>
public record Neo4jConfiguration
{
    public required Neo4jSettings Settings { get; init; }
    public required Neo4jSecrets Secrets { get; init; }

    /// <summary>
    /// Convenience properties for driver configuration
    /// </summary>
    public string ConnectionUri => Secrets.ConnectionUri;
    public string Username => Secrets.Username;
    public string Password => Secrets.Password;
    public string Database => Settings.Database;
    public int MaxConnectionPoolSize => Settings.MaxConnectionPoolSize;
    public int ConnectionTimeoutSeconds => Settings.ConnectionTimeoutSeconds;
    public int MaxTransactionRetryTimeSeconds => Settings.MaxTransactionRetryTimeSeconds;

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(ConnectionUri) &&
                          !string.IsNullOrWhiteSpace(Username) &&
                          !string.IsNullOrWhiteSpace(Password) &&
                          Uri.TryCreate(ConnectionUri, UriKind.Absolute, out _);
}
