using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Reactive Neo4j data access implementation using System.Reactive wrappers
/// Provides backpressure-aware data streaming and flow control around async Neo4j operations
/// </summary>
public class Neo4jReactiveDataAccess : INeo4jReactiveDataAccess, IDisposable
{
    private readonly INeo4jDataAccess _dataAccess;
    private readonly ILogger<Neo4jReactiveDataAccess> _logger;
    private readonly CompositeDisposable _disposables;
    private bool _disposed;

    public Neo4jReactiveDataAccess(
        INeo4jDataAccess dataAccess,
        ILogger<Neo4jReactiveDataAccess> logger)
    {
        _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _disposables = new CompositeDisposable();
    }

    public IObservable<string> UpsertTransactionsReactive(IObservable<Transaction> transactions)
    {
        return Observable.Create<string>(observer =>
        {
            _logger.LogDebug("Starting reactive transaction upsert stream");

            var subscription = transactions
                .Buffer(TimeSpan.FromSeconds(2), 10) // Batch for efficiency
                .Where(batch => batch.Any())
                .SelectMany(batch => Observable.FromAsync(async () =>
                {
                    var results = new List<string>();
                    foreach (var transaction in batch)
                    {
                        try
                        {
                            var transactionId = await _dataAccess.UpsertTransactionAsync(transaction);
                            results.Add(transactionId);
                            _logger.LogTrace("Successfully processed transaction: {TransactionId}", transactionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upsert transaction {TransactionId}", transaction.Id);
                            throw;
                        }
                    }
                    return results;
                }).SelectMany(results => results))
                .Subscribe(
                    onNext: transactionId => observer.OnNext(transactionId),
                    onError: error =>
                    {
                        _logger.LogError(error, "Error in reactive transaction processing stream");
                        observer.OnError(error);
                    },
                    onCompleted: () =>
                    {
                        _logger.LogDebug("Reactive transaction processing stream completed");
                        observer.OnCompleted();
                    });

            return subscription;
        });
    }

    public IObservable<IDictionary<string, object>> GetAnalyticsReactive()
    {
        return Observable.FromAsync(async () =>
        {
            try
            {
                _logger.LogDebug("Getting analytics reactively");
                var analytics = await _dataAccess.GetTransactionAnalyticsAsync();
                _logger.LogDebug("Retrieved analytics with {Count} transactions", 
                    analytics.TryGetValue("totalTransactions", out var count) ? count : 0);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reactive analytics");
                return new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["totalTransactions"] = 0
                } as IDictionary<string, object>;
            }
        });
    }

    public IObservable<Transaction> FindSimilarTransactionsReactive(Transaction transaction)
    {
        return Observable.FromAsync(async () =>
        {
            try
            {
                const string cypher = """
                    MATCH (t:Transaction)
                    WHERE t.id <> $transactionId
                    AND (
                        abs(t.amount - $amount) <= 10.0
                        OR (t.category IS NOT NULL AND t.category = $category)
                    )
                    RETURN t.id as id, 
                           t.date as date, 
                           t.amount as amount, 
                           t.description as description,
                           t.cleanDescription as cleanDescription,
                           t.category as category,
                           t.status as status
                    ORDER BY abs(t.amount - $amount)
                    LIMIT 20
                    """;

                var parameters = new
                {
                    transactionId = transaction.Id,
                    amount = (double)transaction.Amount,
                    category = transaction.Category
                };

                var results = await _dataAccess.ExecuteQueryAsync(cypher, parameters);
                
                return results.Select(record => new Transaction
                {
                    Id = record["id"].ToString() ?? "",
                    Date = DateTime.Parse(record["date"].ToString() ?? DateTime.UtcNow.ToString()),
                    Amount = Convert.ToDecimal(record["amount"]),
                    Description = record["description"]?.ToString() ?? "",
                    CleanDescription = record["cleanDescription"]?.ToString(),
                    Category = record["category"]?.ToString(),
                    Status = Enum.TryParse<ProcessingStatus>(record["status"]?.ToString(), out var status) 
                        ? status : ProcessingStatus.Processed
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find similar transactions for {TransactionId}", transaction.Id);
                return new List<Transaction>();
            }
        }).SelectMany(transactions => transactions);
    }

    public IObservable<IDictionary<string, object>> ExecuteQueryReactive(string cypher, object? parameters = null)
    {
        return Observable.FromAsync(async () =>
        {
            try
            {
                var results = await _dataAccess.ExecuteQueryAsync(cypher, parameters);
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute reactive query");
                return new List<IDictionary<string, object>>
                {
                    new Dictionary<string, object> { ["error"] = ex.Message }
                };
            }
        }).SelectMany(results => results);
    }

    public IObservable<bool> VerifyConnectivityReactive()
    {
        return Observable.FromAsync(async () =>
        {
            try
            {
                var isConnected = await _dataAccess.VerifyConnectivityAsync();
                _logger.LogDebug("Reactive connectivity check: {IsConnected}", isConnected);
                return isConnected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reactive connectivity verification failed");
                return false;
            }
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposables?.Dispose();
            _disposed = true;
            _logger.LogDebug("Neo4j reactive data access disposed");
        }
    }
}