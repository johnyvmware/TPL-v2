using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Components;

/// <summary>
/// Neo4j processor that stores transactions in a graph database and creates relationships
/// </summary>
public class Neo4jProcessor : ProcessorBase<Transaction, Transaction>
{
    private readonly INeo4jDataAccess _neo4jDataAccess;

    public Neo4jProcessor(
        INeo4jDataAccess neo4jDataAccess,
        ILogger<Neo4jProcessor> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        _neo4jDataAccess = neo4jDataAccess ?? throw new ArgumentNullException(nameof(neo4jDataAccess));
    }

    protected override async Task<Transaction> ProcessAsync(Transaction transaction)
    {
        _logger.LogDebug("Processing transaction {Id} for Neo4j storage", transaction.Id);

        try
        {
            // Store the transaction in Neo4j
            var transactionId = await _neo4jDataAccess.UpsertTransactionAsync(transaction);
            _logger.LogDebug("Successfully stored transaction {Id} in Neo4j", transactionId);

            // Create relationships with other transactions
            await _neo4jDataAccess.CreateTransactionRelationshipsAsync(transactionId);
            _logger.LogDebug("Successfully created relationships for transaction {Id}", transactionId);

            // Find and log similar transactions for analytics
            var similarTransactions = await _neo4jDataAccess.FindSimilarTransactionsAsync(transaction);
            var similarCount = similarTransactions.Count();
            
            if (similarCount > 0)
            {
                _logger.LogInformation("Found {Count} similar transactions to {Id} in the graph", 
                    similarCount, transaction.Id);
            }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Neo4jDataAccess implements IAsyncDisposable and will be disposed by DI container
            _logger.LogDebug("Neo4j processor disposed");
        }
        base.Dispose(disposing);
    }
}