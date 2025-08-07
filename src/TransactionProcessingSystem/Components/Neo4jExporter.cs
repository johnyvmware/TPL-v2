using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;
using System.Runtime.CompilerServices;

namespace TransactionProcessingSystem.Components;

/// <summary>
/// Neo4j exporter that stores transactions in a graph database and creates relationships
/// </summary>
public class Neo4jExporter : ProcessorBase<TransactionOld, TransactionOld>
{
    private readonly INeo4jDataAccess _neo4jDataAccess;

    public Neo4jExporter(
        INeo4jDataAccess neo4jDataAccess,
        ILogger<Neo4jExporter> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        _neo4jDataAccess = neo4jDataAccess ?? throw new ArgumentNullException(nameof(neo4jDataAccess));
    }

    protected override async Task<TransactionOld> ProcessAsync(TransactionOld transaction)
    {
        _logger.LogDebug("Processing transaction {Id} for Neo4j storage", transaction.Id);

        try
        {
            // Store the transaction in Neo4j
            var transactionId = await _neo4jDataAccess.UpsertTransactionAsync(transaction);
            _logger.LogDebug("Successfully stored transaction {Id} in Neo4j", transactionId);

            // Relationships are automatically created during upsert
            _logger.LogDebug("Transaction {Id} stored with automatic relationship creation", transactionId);

            // Update transaction status to indicate Neo4j processing is complete
            var processedTransaction = transaction with
            {
                Status = ProcessingStatus.Exported // Using Exported status to indicate graph storage is complete
            };

            _logger.LogDebug("Successfully processed transaction {Id} through Neo4j processor", transaction.Id);
            return processedTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction {Id} in Neo4j processor", transaction.Id);

            // Return the transaction unchanged if Neo4j processing fails
            // This allows the pipeline to continue even if graph storage fails
            return transaction;
        }
    }

    // Additional methods expected by other parts of the codebase
    public async ValueTask<TransactionOld> ProcessItemAsync(TransactionOld transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processing transaction {TransactionId} for Neo4j storage", transaction.Id);

            var storedTransactionId = await _neo4jDataAccess.UpsertTransactionAsync(transaction, cancellationToken);

            _logger.LogTrace("Successfully stored transaction {TransactionId} in Neo4j as {StoredId}",
                transaction.Id, storedTransactionId);

            return transaction with { Status = ProcessingStatus.Processed };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction {TransactionId} in Neo4j", transaction.Id);
            return transaction with { Status = ProcessingStatus.Failed };
        }
    }

    public async ValueTask<TransactionAnalytics> AnalyzeTransactionPatternsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving transaction analytics from Neo4j");
            return await _neo4jDataAccess.GetTransactionAnalyticsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve transaction analytics");
            throw;
        }
    }

    public async IAsyncEnumerable<TransactionOld> FindSimilarTransactionsAsync(
        TransactionOld referenceTransaction,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding similar transactions for {TransactionId}", referenceTransaction.Id);

        await foreach (var similar in _neo4jDataAccess.FindSimilarTransactionsAsync(referenceTransaction, cancellationToken))
        {
            yield return similar;
        }
    }

    public async IAsyncEnumerable<GraphStatistic> GetGraphStatisticsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving graph statistics from Neo4j");

        await foreach (var statistic in _neo4jDataAccess.GetGraphStatisticsAsync(cancellationToken))
        {
            yield return statistic;
        }
    }

    public async IAsyncEnumerable<IDictionary<string, object>> ExecuteCustomAnalyticsAsync(
        string cypherQuery,
        object? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing custom analytics query");

        await foreach (var result in _neo4jDataAccess.ExecuteQueryAsync(cypherQuery, parameters, cancellationToken))
        {
            yield return result;
        }
    }

    public async IAsyncEnumerable<TransactionOld> ProcessTransactionsBatchAsync(
        IAsyncEnumerable<TransactionOld> transactions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting batch processing of transactions to Neo4j");

        await foreach (var result in _neo4jDataAccess.UpsertTransactionsAsync(transactions, cancellationToken))
        {
            var status = result.IsSuccess ? ProcessingStatus.Processed : ProcessingStatus.Failed;

            yield return new TransactionOld
            {
                Id = result.TransactionId,
                Status = status,
                Date = DateTime.UtcNow,
                Amount = 0,
                Description = $"Batch processed at {DateTime.UtcNow}"
            };
        }
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Neo4j exporter");

            var isConnected = await _neo4jDataAccess.VerifyConnectivityAsync(cancellationToken);
            if (!isConnected)
            {
                throw new InvalidOperationException("Failed to connect to Neo4j database");
            }

            await _neo4jDataAccess.InitializeDatabaseAsync(cancellationToken);
            _logger.LogInformation("Neo4j exporter initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Neo4j exporter");
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Neo4jDataAccess implements IAsyncDisposable and will be disposed by DI container
            _logger.LogDebug("Neo4j exporter disposed");
        }
        base.Dispose(disposing);
    }
}