using Neo4j.Driver;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Interface for Neo4j data access operations
/// </summary>
public interface INeo4jDataAccess
{
    /// <summary>
    /// Verifies connectivity to the Neo4j database
    /// </summary>
    Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a transaction node in the graph
    /// </summary>
    Task<string> UpsertTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates relationships between transactions based on common attributes
    /// </summary>
    Task CreateTransactionRelationshipsAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar transactions based on description, amount, and other criteria
    /// </summary>
    Task<IEnumerable<Transaction>> FindSimilarTransactionsAsync(Transaction transaction, double similarityThreshold = 0.8, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction patterns and analytics
    /// </summary>
    Task<IDictionary<string, object>> GetTransactionAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom Cypher query and returns results
    /// </summary>
    Task<IEnumerable<IDictionary<string, object>>> ExecuteQueryAsync(string cypher, object? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates indexes for performance optimization
    /// </summary>
    Task CreateIndexesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets graph statistics and health information
    /// </summary>
    Task<IDictionary<string, object>> GetGraphStatsAsync(CancellationToken cancellationToken = default);
}