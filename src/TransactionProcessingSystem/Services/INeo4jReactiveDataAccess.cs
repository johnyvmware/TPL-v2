using System.Reactive;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Reactive interface for Neo4j operations following official reactive patterns
/// Provides backpressure-aware data streaming and flow control
/// </summary>
public interface INeo4jReactiveDataAccess
{
    /// <summary>
    /// Reactively stream transaction upserts with backpressure control
    /// </summary>
    IObservable<string> UpsertTransactionsReactive(IObservable<Transaction> transactions);

    /// <summary>
    /// Reactively stream transaction analytics updates
    /// </summary>
    IObservable<IDictionary<string, object>> GetAnalyticsReactive();

    /// <summary>
    /// Stream similar transactions for a given transaction reactively
    /// </summary>
    IObservable<Transaction> FindSimilarTransactionsReactive(Transaction transaction);

    /// <summary>
    /// Execute custom Cypher queries reactively with flow control
    /// </summary>
    IObservable<IDictionary<string, object>> ExecuteQueryReactive(string cypher, object? parameters = null);

    /// <summary>
    /// Verify connectivity reactively
    /// </summary>
    IObservable<bool> VerifyConnectivityReactive();
}