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
    /// Initializes database schema with constraints, indexes, and versioning
    /// </summary>
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a transaction node in the graph with proper relationships
    /// </summary>
    Task<string> UpsertTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);



    /// <summary>
    /// Retrieves transaction analytics from the graph database
    /// </summary>
    Task<IDictionary<string, object>> GetTransactionAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes custom Cypher queries
    /// </summary>
    Task<IEnumerable<IDictionary<string, object>>> ExecuteQueryAsync(
        string cypher, 
        object? parameters = null, 
        CancellationToken cancellationToken = default);
}