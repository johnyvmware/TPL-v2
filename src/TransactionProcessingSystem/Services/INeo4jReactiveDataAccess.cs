using System.Reactive;
using System.Threading.Channels;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Modern reactive interface for Neo4j operations using latest C# features
/// Combines IAsyncEnumerable with reactive patterns for optimal streaming performance
/// </summary>
public interface INeo4jReactiveDataAccess
{
    /// <summary>
    /// Reactively stream transaction upserts with modern backpressure control using channels
    /// </summary>
    IObservable<TransactionResult> UpsertTransactionsReactive(
        IAsyncEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactively stream transaction analytics with real-time updates
    /// </summary>
    IObservable<TransactionAnalytics> GetAnalyticsReactive(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream similar transactions reactively with advanced filtering
    /// </summary>
    IObservable<Transaction> FindSimilarTransactionsReactive(
        Transaction referenceTransaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute custom Cypher queries reactively with streaming results and flow control
    /// </summary>
    IObservable<IDictionary<string, object>> ExecuteQueryReactive(
        string cypher,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream graph statistics reactively with real-time updates
    /// </summary>
    IObservable<GraphStatistic> GetGraphStatisticsReactive(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify connectivity reactively with health monitoring
    /// </summary>
    IObservable<ConnectivityStatus> VerifyConnectivityReactive(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a bounded channel for transaction processing with backpressure control
    /// </summary>
    ValueTask<ChannelWriter<Transaction>> CreateTransactionChannelAsync(
        int capacity = 1000,
        BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Modern connectivity status record with detailed information
/// </summary>
public readonly record struct ConnectivityStatus(
    bool IsConnected,
    TimeSpan ResponseTime,
    string? ErrorMessage = null,
    DateTime Timestamp = default)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
};