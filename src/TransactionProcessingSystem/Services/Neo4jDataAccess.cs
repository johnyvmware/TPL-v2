using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using System.Text.Json;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Neo4j data access implementation following official driver best practices
/// </summary>
public class Neo4jDataAccess : INeo4jDataAccess, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jDataAccess> _logger;
    private readonly Neo4jSettings _settings;
    private bool _disposed;

    public Neo4jDataAccess(
        IDriver driver,
        IOptions<Neo4jSettings> settings,
        ILogger<Neo4jDataAccess> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying Neo4j connectivity...");
            await _driver.VerifyConnectivityAsync();
            _logger.LogInformation("Neo4j connectivity verified successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Neo4j connectivity");
            return false;
        }
    }

    public async Task<string> UpsertTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MERGE (t:Transaction {id: $id})
            SET t.date = $date,
                t.amount = $amount,
                t.description = $description,
                t.cleanDescription = $cleanDescription,
                t.emailSubject = $emailSubject,
                t.emailSnippet = $emailSnippet,
                t.category = $category,
                t.status = $status,
                t.updatedAt = datetime()
            ON CREATE SET t.createdAt = datetime()
            RETURN t.id as transactionId
            """;

        var parameters = new
        {
            id = transaction.Id,
            date = transaction.Date.ToString("yyyy-MM-dd"),
            amount = (double)transaction.Amount,
            description = transaction.Description,
            cleanDescription = transaction.CleanDescription,
            emailSubject = transaction.EmailSubject,
            emailSnippet = transaction.EmailSnippet,
            category = transaction.Category,
            status = transaction.Status.ToString()
        };

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Write));
            
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters);
                var record = await cursor.SingleAsync();
                return record["transactionId"].As<string>();
            });

            _logger.LogDebug("Successfully upserted transaction {TransactionId}", transaction.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert transaction {TransactionId}", transaction.Id);
            throw;
        }
    }

    public async Task CreateTransactionRelationshipsAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (t1:Transaction {id: $transactionId})
            MATCH (t2:Transaction)
            WHERE t1 <> t2 AND t1.id <> t2.id
            
            // Create SIMILAR_AMOUNT relationship
            WITH t1, t2
            WHERE abs(t1.amount - t2.amount) <= 10.0
            MERGE (t1)-[r1:SIMILAR_AMOUNT]-(t2)
            SET r1.difference = abs(t1.amount - t2.amount)
            
            WITH t1, t2
            // Create SAME_CATEGORY relationship
            WHERE t1.category IS NOT NULL AND t2.category IS NOT NULL 
            AND t1.category = t2.category
            MERGE (t1)-[r2:SAME_CATEGORY]-(t2)
            
            WITH t1, t2
            // Create SIMILAR_DESCRIPTION relationship (basic string similarity)
            WHERE t1.cleanDescription IS NOT NULL AND t2.cleanDescription IS NOT NULL
            AND size(t1.cleanDescription) > 5 AND size(t2.cleanDescription) > 5
            AND apoc.text.jaroWinklerDistance(t1.cleanDescription, t2.cleanDescription) > 0.8
            MERGE (t1)-[r3:SIMILAR_DESCRIPTION]-(t2)
            SET r3.similarity = apoc.text.jaroWinklerDistance(t1.cleanDescription, t2.cleanDescription)
            
            RETURN count(*) as relationshipsCreated
            """;

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Write));
            
            await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, new { transactionId });
                var record = await cursor.SingleAsync();
                var count = record["relationshipsCreated"].As<int>();
                _logger.LogDebug("Created {Count} relationships for transaction {TransactionId}", count, transactionId);
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create relationships for transaction {TransactionId}. This may be due to missing APOC procedures.", transactionId);
            // Continue execution as relationships are optional
        }
    }

    public async Task<IEnumerable<Transaction>> FindSimilarTransactionsAsync(
        Transaction transaction, 
        double similarityThreshold = 0.8, 
        CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (t:Transaction)
            WHERE t.id <> $transactionId
            AND (
                // Similar amounts (within 10% or $10)
                abs(t.amount - $amount) <= greatest($amount * 0.1, 10.0)
                OR
                // Same category
                (t.category IS NOT NULL AND t.category = $category)
                OR
                // Similar description (fallback to simple contains check if APOC not available)
                (t.cleanDescription IS NOT NULL AND 
                 (t.cleanDescription CONTAINS $description OR $description CONTAINS t.cleanDescription))
            )
            RETURN t.id as id, 
                   t.date as date, 
                   t.amount as amount, 
                   t.description as description,
                   t.cleanDescription as cleanDescription,
                   t.emailSubject as emailSubject,
                   t.emailSnippet as emailSnippet,
                   t.category as category,
                   t.status as status
            ORDER BY abs(t.amount - $amount)
            LIMIT 20
            """;

        var parameters = new
        {
            transactionId = transaction.Id,
            amount = (double)transaction.Amount,
            category = transaction.Category,
            description = transaction.CleanDescription ?? transaction.Description
        };

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters);
                var records = await cursor.ToListAsync();
                
                return records.Select(record => new Transaction
                {
                    Id = record["id"].As<string>(),
                    Date = DateTime.Parse(record["date"].As<string>()),
                    Amount = (decimal)record["amount"].As<double>(),
                    Description = record["description"].As<string>(),
                    CleanDescription = record["cleanDescription"]?.As<string>(),
                    EmailSubject = record["emailSubject"]?.As<string>(),
                    EmailSnippet = record["emailSnippet"]?.As<string>(),
                    Category = record["category"]?.As<string>(),
                    Status = Enum.Parse<ProcessingStatus>(record["status"].As<string>())
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar transactions for {TransactionId}", transaction.Id);
            return Enumerable.Empty<Transaction>();
        }
    }

    public async Task<IDictionary<string, object>> GetTransactionAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        const string cypher = """
            MATCH (t:Transaction)
            RETURN 
                count(t) as totalTransactions,
                sum(t.amount) as totalAmount,
                avg(t.amount) as averageAmount,
                min(t.amount) as minAmount,
                max(t.amount) as maxAmount,
                collect(DISTINCT t.category) as categories,
                collect(DISTINCT t.status) as statuses
            """;

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher);
                var record = await cursor.SingleAsync();
                
                return new Dictionary<string, object>
                {
                    ["totalTransactions"] = record["totalTransactions"].As<long>(),
                    ["totalAmount"] = record["totalAmount"].As<double?>() ?? 0.0,
                    ["averageAmount"] = record["averageAmount"].As<double?>() ?? 0.0,
                    ["minAmount"] = record["minAmount"].As<double?>() ?? 0.0,
                    ["maxAmount"] = record["maxAmount"].As<double?>() ?? 0.0,
                    ["categories"] = record["categories"].As<List<object>>().Where(x => x != null).ToList(),
                    ["statuses"] = record["statuses"].As<List<object>>().Where(x => x != null).ToList()
                };
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction analytics");
            return new Dictionary<string, object>();
        }
    }

    public async Task<IEnumerable<IDictionary<string, object>>> ExecuteQueryAsync(
        string cypher, 
        object? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters);
                var records = await cursor.ToListAsync();
                
                return records.Select(record => 
                    record.Keys.ToDictionary(key => key, key => record[key].As<object>()));
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute custom query: {Cypher}", cypher);
            throw;
        }
    }

    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexCommands = new[]
        {
            "CREATE INDEX transaction_id_index IF NOT EXISTS FOR (t:Transaction) ON (t.id)",
            "CREATE INDEX transaction_category_index IF NOT EXISTS FOR (t:Transaction) ON (t.category)",
            "CREATE INDEX transaction_amount_index IF NOT EXISTS FOR (t:Transaction) ON (t.amount)",
            "CREATE INDEX transaction_date_index IF NOT EXISTS FOR (t:Transaction) ON (t.date)",
            "CREATE INDEX transaction_status_index IF NOT EXISTS FOR (t:Transaction) ON (t.status)"
        };

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Write));
            
            foreach (var command in indexCommands)
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(command);
                    return Task.CompletedTask;
                });
            }
            
            _logger.LogInformation("Successfully created all Neo4j indexes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Neo4j indexes");
            throw;
        }
    }

    public async Task<IDictionary<string, object>> GetGraphStatsAsync(CancellationToken cancellationToken = default)
    {
        const string cypher = """
            CALL apoc.meta.stats()
            YIELD labels, relTypesCount, nodeCount, relCount
            RETURN labels, relTypesCount, nodeCount, relCount
            """;

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher);
                var record = await cursor.SingleAsync();
                
                return new Dictionary<string, object>
                {
                    ["labels"] = record["labels"].As<IDictionary<string, object>>(),
                    ["relationshipTypes"] = record["relTypesCount"].As<IDictionary<string, object>>(),
                    ["nodeCount"] = record["nodeCount"].As<long>(),
                    ["relationshipCount"] = record["relCount"].As<long>()
                };
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get graph stats (APOC may not be available). Falling back to basic stats.");
            
            // Fallback to basic statistics
            const string basicCypher = """
                MATCH (n) 
                OPTIONAL MATCH ()-[r]->()
                RETURN count(DISTINCT n) as nodeCount, count(r) as relCount
                """;
            
            await using var fallbackSession = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            return await fallbackSession.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(basicCypher);
                var record = await cursor.SingleAsync();
                
                return new Dictionary<string, object>
                {
                    ["nodeCount"] = record["nodeCount"].As<long>(),
                    ["relationshipCount"] = record["relCount"].As<long>(),
                    ["labels"] = new Dictionary<string, object>(),
                    ["relationshipTypes"] = new Dictionary<string, object>()
                };
            });
        }
    }



    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_driver != null)
            {
                await _driver.DisposeAsync();
            }
            _disposed = true;
            _logger.LogDebug("Neo4j driver disposed");
        }
    }
}