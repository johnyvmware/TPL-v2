using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Processors;
using TransactionProcessingSystem.Services;
using System.Runtime.CompilerServices;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Simple transaction fetcher interface for modern pipeline
/// </summary>
public interface ITransactionFetcher
{
    IAsyncEnumerable<Transaction> FetchTransactionsAsync(
        ProcessingOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Modern transaction processing pipeline using latest C# language features
/// Primary constructor, IAsyncEnumerable streaming, and ValueTask performance
/// Simplified to work without complex base classes
/// </summary>
public sealed class TransactionPipeline(
    ITransactionFetcher fetcher,
    Neo4jExporter neo4jExporter,
    ILogger<TransactionPipeline> logger)
{
    public async ValueTask<ProcessingResult> ProcessAsync(
        ProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var processedCount = 0;
        var failedCount = 0;

        try
        {
            logger.LogInformation("Starting modern transaction pipeline processing with batch size {BatchSize}",
                options.BatchSize);

            // Use async enumerable streaming for memory efficiency
            var transactions = fetcher.FetchTransactionsAsync(options, cancellationToken);

            await foreach (var processedTransaction in ProcessTransactionsStreamAsync(transactions, cancellationToken))
            {
                if (processedTransaction.Status == ProcessingStatus.Processed)
                {
                    processedCount++;
                    logger.LogTrace("Successfully processed transaction: {TransactionId}", processedTransaction.Id);
                }
                else
                {
                    failedCount++;
                    logger.LogWarning("Failed to process transaction: {TransactionId} with status {Status}",
                        processedTransaction.Id, processedTransaction.Status);
                }

                // Respect cancellation
                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation(
                "Pipeline processing completed: {ProcessedCount} processed, {FailedCount} failed in {Duration}ms",
                processedCount, failedCount, duration.TotalMilliseconds);

            return new ProcessingResult
            {
                TotalProcessed = processedCount,
                TotalFailed = failedCount,
                Duration = duration,
                Status = cancellationToken.IsCancellationRequested ? ProcessingStatus.Cancelled : ProcessingStatus.Processed
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline processing failed after processing {ProcessedCount} transactions", processedCount);

            return new ProcessingResult
            {
                TotalProcessed = processedCount,
                TotalFailed = failedCount + 1,
                Duration = DateTime.UtcNow - startTime,
                Status = ProcessingStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    public IAsyncEnumerable<Transaction> ProcessTransactionsStreamAsync(
        IAsyncEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Starting transaction stream processing through Neo4j processor");

        return ProcessTransactionsStreamInternalAsync(transactions, cancellationToken);
    }

    private async IAsyncEnumerable<Transaction> ProcessTransactionsStreamInternalAsync(
        IAsyncEnumerable<Transaction> transactions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var transaction in transactions.WithCancellation(cancellationToken))
        {
            Transaction processedTransaction;
            try
            {
                // Process through Neo4j processor using modern async patterns
                processedTransaction = await neo4jExporter.ProcessItemAsync(transaction, cancellationToken)
    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process transaction {TransactionId}", transaction.Id);
                processedTransaction = transaction with { Status = ProcessingStatus.Failed };
            }

            yield return processedTransaction;

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        logger.LogDebug("Completed transaction stream processing");
    }

    public async ValueTask<ProcessingResult> ProcessBatchAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing batch of {TransactionCount} transactions", transactions.Count());

        // Convert to async enumerable using custom extension
        var asyncTransactions = ConvertToAsyncEnumerable(transactions);

        var processedTransactions = new List<Transaction>();

        await foreach (var processedTransaction in ProcessTransactionsStreamAsync(asyncTransactions, cancellationToken))
        {
            processedTransactions.Add(processedTransaction);
        }

        var processedCount = processedTransactions.Count(t => t.Status == ProcessingStatus.Processed);
        var failedCount = processedTransactions.Count(t => t.Status == ProcessingStatus.Failed);

        return new ProcessingResult
        {
            TotalProcessed = processedCount,
            TotalFailed = failedCount,
            Duration = TimeSpan.Zero, // Could be measured if needed
            Status = failedCount == 0 ? ProcessingStatus.Processed : ProcessingStatus.Failed
        };
    }

    public async ValueTask<TransactionAnalytics> GetPipelineAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Retrieving pipeline analytics from Neo4j processor");

            return await neo4jExporter.AnalyzeTransactionPatternsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve pipeline analytics");
            throw;
        }
    }

    public async IAsyncEnumerable<Transaction> FindSimilarTransactionsAsync(
        Transaction referenceTransaction,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Finding similar transactions for {TransactionId} through pipeline", referenceTransaction.Id);

        await foreach (var similar in neo4jExporter.FindSimilarTransactionsAsync(referenceTransaction, cancellationToken))
        {
            yield return similar;
        }
    }

    public async IAsyncEnumerable<GraphStatistic> GetGraphStatisticsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Retrieving graph statistics through pipeline");

        await foreach (var statistic in neo4jExporter.GetGraphStatisticsAsync(cancellationToken))
        {
            yield return statistic;
        }
    }

    public async IAsyncEnumerable<IDictionary<string, object>> ExecuteCustomAnalyticsAsync(
        string cypherQuery,
        object? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Executing custom analytics query through pipeline");

        await foreach (var result in neo4jExporter.ExecuteCustomAnalyticsAsync(cypherQuery, parameters, cancellationToken))
        {
            yield return result;
        }
    }

    /// <summary>
    /// Processes transactions using high-performance batch streaming with custom chunking
    /// </summary>
    public async IAsyncEnumerable<Transaction> ProcessTransactionsBatchStreamAsync(
        IAsyncEnumerable<Transaction> transactions,
        int batchSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting high-performance batch stream processing with batch size {BatchSize}", batchSize);

        await foreach (var batch in ChunkAsyncEnumerable(transactions, batchSize, cancellationToken))
        {
            // Process each batch through the Neo4j processor for high throughput
            await foreach (var processed in neo4jExporter.ProcessTransactionsBatchAsync(ConvertToAsyncEnumerable(batch), cancellationToken))
            {
                yield return processed;
            }

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        logger.LogDebug("Completed high-performance batch stream processing");
    }

    /// <summary>
    /// Gets processing statistics using modern async patterns
    /// </summary>
    public async ValueTask<PipelineStatistics> GetProcessingStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await GetPipelineAnalyticsAsync(cancellationToken).ConfigureAwait(false);

            var statistics = new List<GraphStatistic>();
            await foreach (var stat in GetGraphStatisticsAsync(cancellationToken))
            {
                statistics.Add(stat);
            }

            return new PipelineStatistics
            {
                TotalTransactions = analytics.TotalTransactions,
                TotalAmount = analytics.TotalAmount,
                ProcessingEfficiency = CalculateProcessingEfficiency(analytics),
                GraphNodeCount = statistics.Where(s => s.Type == "Node").Sum(s => s.Count),
                GraphRelationshipCount = statistics.Where(s => s.Type == "Relationship").Sum(s => s.Count),
                LastProcessingTime = DateTime.UtcNow,
                DatabaseVersion = GetDatabaseVersion()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve processing statistics");
            throw;
        }
    }

    // Helper methods for missing extension methods
    private static IAsyncEnumerable<T> ConvertToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        return ConvertToAsyncEnumerableInternal(source);
    }

    private static async IAsyncEnumerable<T> ConvertToAsyncEnumerableInternal<T>(IEnumerable<T> source)
    {
        await Task.Yield(); // Make it truly async to avoid compiler warning

        foreach (var item in source)
        {
            yield return item;
        }
    }

    private static async IAsyncEnumerable<IList<T>> ChunkAsyncEnumerable<T>(
        IAsyncEnumerable<T> source,
        int chunkSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chunk = new List<T>(chunkSize);

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            chunk.Add(item);

            if (chunk.Count == chunkSize)
            {
                yield return chunk;
                chunk = new List<T>(chunkSize);
            }
        }

        if (chunk.Count > 0)
        {
            yield return chunk;
        }
    }

    private static decimal CalculateProcessingEfficiency(TransactionAnalytics analytics)
    {
        if (analytics.TotalTransactions == 0) return 0;

        // Simple efficiency calculation based on relationships vs transactions
        var relationshipDensity = (analytics.Relationships.CategorySimilarities + analytics.Relationships.AmountSimilarities)
                                 / (decimal)Math.Max(analytics.TotalTransactions, 1);

        return Math.Min(relationshipDensity * 100, 100); // Cap at 100%
    }

    private static string GetDatabaseVersion() => $"v{DateTime.UtcNow:yyyy.MM.dd}";
}

/// <summary>
/// Extended processing result with modern record syntax
/// </summary>
public sealed record ProcessingResult
{
    public required int TotalProcessed { get; init; }
    public required int TotalFailed { get; init; }
    public required TimeSpan Duration { get; init; }
    public required ProcessingStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Pipeline statistics record with comprehensive metrics
/// </summary>
public sealed record PipelineStatistics
{
    public required long TotalTransactions { get; init; }
    public required decimal TotalAmount { get; init; }
    public required decimal ProcessingEfficiency { get; init; }
    public required long GraphNodeCount { get; init; }
    public required long GraphRelationshipCount { get; init; }
    public required DateTime LastProcessingTime { get; init; }
    public required string DatabaseVersion { get; init; }
}

/// <summary>
/// Processing options with modern features
/// </summary>
public sealed record ProcessingOptions
{
    public int BatchSize { get; init; } = 100;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string[]? Categories { get; init; } = [];
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public bool EnableParallelProcessing { get; init; } = true;
    public int MaxConcurrency { get; init; } = Environment.ProcessorCount;
}