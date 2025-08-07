using Neo4j.Driver;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Modern Neo4j data access interface using latest C# language features
/// </summary>
public interface INeo4jDataAccess
{
    /// <summary>
    /// Verifies connectivity to the Neo4j database with modern async patterns
    /// </summary>
    ValueTask<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes database schema with constraints, indexes, and versioning
    /// </summary>
    ValueTask InitializeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a transaction node in the graph with proper relationships
    /// </summary>
    ValueTask<string> UpsertTransactionAsync(TransactionOld transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams multiple transaction upserts with async enumerable for high performance
    /// </summary>
    IAsyncEnumerable<TransactionResult> UpsertTransactionsAsync(
        IAsyncEnumerable<TransactionOld> transactions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams similar transactions for a given transaction using async enumerable
    /// </summary>
    IAsyncEnumerable<TransactionOld> FindSimilarTransactionsAsync(
        TransactionOld transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transaction analytics from the graph database
    /// </summary>
    ValueTask<TransactionAnalytics> GetTransactionAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes custom Cypher queries with streaming results
    /// </summary>
    IAsyncEnumerable<IDictionary<string, object>> ExecuteQueryAsync(
        string cypher,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams graph statistics and metadata
    /// </summary>
    IAsyncEnumerable<GraphStatistic> GetGraphStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Modern result record for transaction operations
/// </summary>
public readonly record struct TransactionResult(string TransactionId, bool IsSuccess, string? ErrorMessage = null);

/// <summary>
/// Strongly-typed transaction analytics with modern record syntax
/// </summary>
public sealed record TransactionAnalytics
{
    public required long TotalTransactions { get; init; }
    public required decimal TotalAmount { get; init; }
    public required decimal AverageAmount { get; init; }
    public required decimal MinAmount { get; init; }
    public required decimal MaxAmount { get; init; }
    public required string[] Categories { get; init; } = [];
    public required int UniqueCategories { get; init; }
    public required DateRange DateRange { get; init; }
    public required RelationshipStats Relationships { get; init; }
    public required long WeekendTransactions { get; init; }
    public required CategoryBreakdown[] TopCategories { get; init; } = [];
}

/// <summary>
/// Date range information using modern record syntax
/// </summary>
public readonly record struct DateRange(
    DateTime? Earliest,
    DateTime? Latest,
    int UniqueDays,
    int UniqueMonths,
    int UniqueYears);

/// <summary>
/// Relationship statistics record
/// </summary>
public readonly record struct RelationshipStats(
    long CategorySimilarities,
    long AmountSimilarities);

/// <summary>
/// Category breakdown with count
/// </summary>
public readonly record struct CategoryBreakdown(string Category, long Count);

/// <summary>
/// Graph statistics record
/// </summary>
public readonly record struct GraphStatistic(
    string Type,
    string Name,
    long Count,
    IDictionary<string, object>? Metadata = null);