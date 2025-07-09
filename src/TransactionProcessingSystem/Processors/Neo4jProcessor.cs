using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Processors;

/// <summary>
/// Modern Neo4j transaction processor using latest C# language features
/// Primary constructor, IAsyncEnumerable streaming, and ValueTask performance
/// </summary>
public sealed class Neo4jProcessor(
    INeo4jDataAccess neo4jDataAccess,
    ILogger<Neo4jProcessor> logger)
{
    protected ILogger<Neo4jProcessor> Logger => logger;

    public async ValueTask<Transaction> ProcessItemAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Processing transaction {TransactionId} for Neo4j storage", transaction.Id);

            var storedTransactionId = await neo4jDataAccess.UpsertTransactionAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            Logger.LogTrace("Successfully stored transaction {TransactionId} in Neo4j as {StoredId}", 
                transaction.Id, storedTransactionId);

            // Return the processed transaction for further pipeline processing
            return transaction with { Status = ProcessingStatus.Processed };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process transaction {TransactionId} in Neo4j", transaction.Id);
            
            // Return the transaction marked as failed
            return transaction with { Status = ProcessingStatus.Failed };
        }
    }

    /// <summary>
    /// Batch processes multiple transactions using async enumerable streaming
    /// </summary>
    public async IAsyncEnumerable<Transaction> ProcessTransactionsBatchAsync(
        IAsyncEnumerable<Transaction> transactions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting batch processing of transactions to Neo4j");

        var processedCount = 0;
        var failedCount = 0;

        await foreach (var result in neo4jDataAccess.UpsertTransactionsAsync(transactions, cancellationToken))
        {
            var status = result.IsSuccess ? ProcessingStatus.Processed : ProcessingStatus.Failed;
            
            if (result.IsSuccess)
            {
                processedCount++;
                Logger.LogTrace("Successfully processed transaction: {TransactionId}", result.TransactionId);
            }
            else
            {
                failedCount++;
                Logger.LogWarning("Failed to process transaction {TransactionId}: {Error}", 
                    result.TransactionId, result.ErrorMessage);
            }

            // Create a processed transaction (you might need to get the original transaction data)
            yield return new Transaction
            {
                Id = result.TransactionId,
                Status = status,
                // Note: In a real implementation, you'd need to maintain a mapping or pass through original data
                Date = DateTime.UtcNow,
                Amount = 0,
                Description = $"Batch processed at {DateTime.UtcNow}"
            };
        }

        Logger.LogInformation(
            "Completed batch processing: {ProcessedCount} successful, {FailedCount} failed",
            processedCount, failedCount);
    }

    /// <summary>
    /// Analyzes transaction patterns using graph data
    /// </summary>
    public async ValueTask<TransactionAnalytics> AnalyzeTransactionPatternsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Retrieving transaction analytics from Neo4j");
            
            var analytics = await neo4jDataAccess.GetTransactionAnalyticsAsync(cancellationToken)
                .ConfigureAwait(false);

            Logger.LogInformation(
                "Retrieved analytics: {TotalTransactions} transactions, {TotalAmount:C} total amount",
                analytics.TotalTransactions, analytics.TotalAmount);

            return analytics;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve transaction analytics");
            throw;
        }
    }

    /// <summary>
    /// Finds similar transactions using graph relationships  
    /// </summary>
    public async IAsyncEnumerable<Transaction> FindSimilarTransactionsAsync(
        Transaction referenceTransaction,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Finding similar transactions for {TransactionId}", referenceTransaction.Id);

        var similarCount = 0;

        await foreach (var similar in neo4jDataAccess.FindSimilarTransactionsAsync(referenceTransaction, cancellationToken))
        {
            similarCount++;
            Logger.LogTrace("Found similar transaction: {SimilarId} (Amount: {Amount:C})", 
                similar.Id, similar.Amount);
            
            yield return similar;
        }

        Logger.LogDebug("Found {SimilarCount} similar transactions for {TransactionId}", 
            similarCount, referenceTransaction.Id);
    }

    /// <summary>
    /// Gets graph statistics using async enumerable streaming
    /// </summary>
    public async IAsyncEnumerable<GraphStatistic> GetGraphStatisticsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Retrieving graph statistics from Neo4j");

        await foreach (var statistic in neo4jDataAccess.GetGraphStatisticsAsync(cancellationToken))
        {
            Logger.LogTrace("Graph statistic: {Type} {Name} = {Count}", 
                statistic.Type, statistic.Name, statistic.Count);
            
            yield return statistic;
        }
    }

    /// <summary>
    /// Executes custom analytics queries with streaming results
    /// </summary>
    public async IAsyncEnumerable<IDictionary<string, object>> ExecuteCustomAnalyticsAsync(
        string cypherQuery,
        object? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing custom analytics query");

        try
        {
            await foreach (var result in neo4jDataAccess.ExecuteQueryAsync(cypherQuery, parameters, cancellationToken))
            {
                yield return result;
            }

            Logger.LogDebug("Completed custom analytics query execution");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute custom analytics query");
            throw;
        }
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Initializing Neo4j processor");

            // Verify connectivity using modern async pattern
            var isConnected = await neo4jDataAccess.VerifyConnectivityAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!isConnected)
            {
                throw new InvalidOperationException("Failed to connect to Neo4j database");
            }

            // Initialize database schema
            await neo4jDataAccess.InitializeDatabaseAsync(cancellationToken).ConfigureAwait(false);

            Logger.LogInformation("Neo4j processor initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Neo4j processor");
            throw;
        }
    }
}