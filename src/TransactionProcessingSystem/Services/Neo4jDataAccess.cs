using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Modern Neo4j data access implementation using latest C# language features
/// Primary constructor with DI, IAsyncEnumerable streaming, and ValueTask performance
/// </summary>
public sealed class Neo4jDataAccess(
    IDriver driver,
    IOptions<Neo4jSettings> settings,
    ILogger<Neo4jDataAccess> logger) : INeo4jDataAccess, IAsyncDisposable
{
    private readonly Neo4jSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private bool _disposed;

    public async ValueTask<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Verifying Neo4j connectivity");
            
            await using var session = driver.AsyncSession(ConfigureSession());
                
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1 AS test").ConfigureAwait(false);
                var record = await cursor.SingleAsync(cancellationToken).ConfigureAwait(false);
                return record["test"].As<int>();
            }, ConfigureTransaction("connectivity_check"), cancellationToken).ConfigureAwait(false);
                
            logger.LogInformation("Neo4j connectivity verified successfully");
            return result == 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Neo4j connectivity verification failed");
            return false;
        }
    }

    public async ValueTask InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        string[] constraints = 
        [
            "CREATE CONSTRAINT transaction_id_unique IF NOT EXISTS FOR (t:Transaction) REQUIRE t.id IS UNIQUE",
            "CREATE CONSTRAINT category_name_unique IF NOT EXISTS FOR (c:Category) REQUIRE c.name IS UNIQUE",
            "CREATE CONSTRAINT year_value_unique IF NOT EXISTS FOR (y:Year) REQUIRE y.value IS UNIQUE",
            "CREATE CONSTRAINT month_composite_unique IF NOT EXISTS FOR (m:Month) REQUIRE (m.value, m.year) IS UNIQUE",
            "CREATE CONSTRAINT day_date_unique IF NOT EXISTS FOR (d:Day) REQUIRE d.date IS UNIQUE",
            "CREATE CONSTRAINT db_version_unique IF NOT EXISTS FOR (v:DatabaseVersion) REQUIRE v.version IS UNIQUE"
        ];

        string[] indexes = 
        [
            "CREATE INDEX transaction_amount_idx IF NOT EXISTS FOR (t:Transaction) ON (t.amount)",
            "CREATE INDEX transaction_date_idx IF NOT EXISTS FOR (t:Transaction) ON (t.date)",
            "CREATE INDEX transaction_hash_idx IF NOT EXISTS FOR (t:Transaction) ON (t.contentHash)",
            "CREATE INDEX category_normalized_idx IF NOT EXISTS FOR (c:Category) ON (c.normalizedName)",
            "CREATE FULLTEXT INDEX transaction_description_fulltext IF NOT EXISTS FOR (t:Transaction) ON EACH [t.description, t.cleanDescription]"
        ];

        try
        {
            await using var session = driver.AsyncSession(ConfigureSession(AccessMode.Write));

            await session.ExecuteWriteAsync(async tx =>
            {
                // Create constraints first using modern collection iteration
                await foreach (var constraint in constraints.ToAsyncEnumerable())
                {
                    try
                    {
                        await tx.RunAsync(constraint).ConfigureAwait(false);
                        logger.LogDebug("Created constraint: {Constraint}", constraint);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Constraint already exists or failed: {Constraint}", constraint);
                    }
                }

                // Create indexes using modern async patterns
                await foreach (var index in indexes.ToAsyncEnumerable())
                {
                    try
                    {
                        await tx.RunAsync(index).ConfigureAwait(false);
                        logger.LogDebug("Created index: {Index}", index);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Index already exists or failed: {Index}", index);
                    }
                }

                return ValueTask.CompletedTask;
            }, ConfigureTransaction("schema_initialization", GetDatabaseVersion()), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Neo4j database schema initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Neo4j database schema");
            throw;
        }
    }

    public async ValueTask<string> UpsertTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        const string cypher = """
            // Create or get the database version node
            MERGE (dbVersion:DatabaseVersion {
                version: $dbVersion,
                createdDate: date($today)
            })
            
            // Create or get the category node
            MERGE (category:Category {
                name: $category,
                normalizedName: toLower(trim($category))
            })
            
            // Create hierarchical date structure with modern graph patterns
            MERGE (year:Year { value: $year })
            MERGE (month:Month { value: $month, year: $year })
            MERGE (day:Day {
                date: date($transactionDate),
                dayOfWeek: $dayOfWeek,
                dayOfMonth: $dayOfMonth,
                isWeekend: $isWeekend
            })
            
            // Create relationships between date nodes
            MERGE (year)-[:HAS_MONTH]->(month)
            MERGE (month)-[:HAS_DAY]->(day)
            
            // Create or update transaction with modern ON CREATE/MATCH patterns
            MERGE (transaction:Transaction { id: $transactionId })
            ON CREATE SET
                transaction.amount = $amount,
                transaction.description = $description,
                transaction.cleanDescription = $cleanDescription,
                transaction.date = date($transactionDate),
                transaction.status = $status,
                transaction.contentHash = $contentHash,
                transaction.createdAt = datetime(),
                transaction.version = $dbVersion
            ON MATCH SET
                transaction.amount = $amount,
                transaction.description = $description,
                transaction.cleanDescription = $cleanDescription,
                transaction.status = $status,
                transaction.updatedAt = datetime()
            
            // Create relationships
            MERGE (transaction)-[:BELONGS_TO_CATEGORY]->(category)
            MERGE (transaction)-[:OCCURRED_ON]->(day)
            MERGE (transaction)-[:STORED_IN_VERSION]->(dbVersion)
            
            // Create similarity relationships automatically
            WITH transaction, category
            MATCH (other:Transaction)-[:BELONGS_TO_CATEGORY]->(category)
            WHERE other.id <> transaction.id
            MERGE (transaction)-[:SAME_CATEGORY]->(other)
            
            // Create amount similarity relationships
            WITH transaction
            MATCH (similar:Transaction)
            WHERE similar.id <> transaction.id
              AND (
                  abs(similar.amount - transaction.amount) <= 10.0
                  OR abs(similar.amount - transaction.amount) / transaction.amount <= 0.1
              )
            MERGE (transaction)-[:SIMILAR_AMOUNT]->(similar)
            
            RETURN transaction.id AS transactionId
            """;

        var transactionDate = transaction.Date;
        var parameters = new
        {
            transactionId = transaction.Id,
            amount = (double)transaction.Amount,
            category = transaction.Category ?? "Unknown",
            description = transaction.Description ?? "",
            cleanDescription = transaction.CleanDescription ?? transaction.Description ?? "",
            status = transaction.Status.ToString(),
            transactionDate = transactionDate.ToString("yyyy-MM-dd"),
            year = transactionDate.Year,
            month = transactionDate.Month,
            dayOfWeek = transactionDate.DayOfWeek.ToString(),
            dayOfMonth = transactionDate.Day,
            isWeekend = transactionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            today = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            dbVersion = GetDatabaseVersion(),
            contentHash = GenerateTransactionHash(transaction)
        };

        try
        {
            await using var session = driver.AsyncSession(ConfigureSession(AccessMode.Write));
            
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters).ConfigureAwait(false);
                var record = await cursor.SingleAsync(cancellationToken).ConfigureAwait(false);
                return record["transactionId"].As<string>();
            }, ConfigureTransaction("upsert_transaction", transaction.Id), cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Successfully upserted transaction {TransactionId} with relationships", transaction.Id);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert transaction {TransactionId}", transaction.Id);
            throw;
        }
    }

    public async IAsyncEnumerable<TransactionResult> UpsertTransactionsAsync(
        IAsyncEnumerable<Transaction> transactions, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var session = driver.AsyncSession(ConfigureSession(AccessMode.Write));

        await foreach (var transaction in transactions.WithCancellation(cancellationToken))
        {
            TransactionResult result;
            try
            {
                var transactionId = await UpsertSingleTransactionInSession(session, transaction, cancellationToken).ConfigureAwait(false);
                result = new TransactionResult(transactionId, true);
                logger.LogTrace("Successfully processed transaction: {TransactionId}", transactionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process transaction {TransactionId}", transaction.Id);
                result = new TransactionResult(transaction.Id, false, ex.Message);
            }

            yield return result;
        }
    }

    public async IAsyncEnumerable<Transaction> FindSimilarTransactionsAsync(
        Transaction transaction, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        await using var session = driver.AsyncSession(ConfigureSession());

        await foreach (var record in ExecuteQueryInternalAsync(session, cypher, parameters, cancellationToken))
        {
            yield return record.TryGetValue("id", out var id) && id is not null
                ? new Transaction
                {
                    Id = id.ToString() ?? "",
                    Date = DateTime.TryParse(record["date"]?.ToString(), out var date) ? date : DateTime.UtcNow,
                    Amount = Convert.ToDecimal(record["amount"]),
                    Description = record["description"]?.ToString() ?? "",
                    CleanDescription = record["cleanDescription"]?.ToString(),
                    Category = record["category"]?.ToString(),
                    Status = Enum.TryParse<ProcessingStatus>(record["status"]?.ToString(), out var status) 
                        ? status : ProcessingStatus.Processed
                }
                : throw new InvalidOperationException($"Invalid transaction record for {transaction.Id}");
        }
    }

    public async ValueTask<TransactionAnalytics> GetTransactionAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        const string cypher = """
            // Get comprehensive transaction analytics with graph insights
            MATCH (t:Transaction)
            OPTIONAL MATCH (t)-[:BELONGS_TO_CATEGORY]->(c:Category)
            OPTIONAL MATCH (t)-[:OCCURRED_ON]->(d:Day)-[:HAS_MONTH]->(m:Month)-[:HAS_YEAR]->(y:Year)
            OPTIONAL MATCH (t)-[:SAME_CATEGORY]->(sameCat:Transaction)
            OPTIONAL MATCH (t)-[:SIMILAR_AMOUNT]->(similarAmt:Transaction)
            
            RETURN {
                totalTransactions: count(DISTINCT t),
                totalAmount: sum(t.amount),
                averageAmount: avg(t.amount),
                minAmount: min(t.amount),
                maxAmount: max(t.amount),
                categories: collect(DISTINCT c.name),
                uniqueCategories: count(DISTINCT c),
                dateRange: {
                    earliest: min(t.date),
                    latest: max(t.date),
                    uniqueDays: count(DISTINCT d),
                    uniqueMonths: count(DISTINCT m),
                    uniqueYears: count(DISTINCT y)
                },
                relationships: {
                    categorySimilarities: count(DISTINCT sameCat),
                    amountSimilarities: count(DISTINCT similarAmt)
                },
                weekendTransactions: count(DISTINCT CASE WHEN d.isWeekend THEN t END),
                topCategories: collect(DISTINCT { 
                    category: c.name, 
                    count: count(DISTINCT t) 
                })[0..5]
            } AS analytics
            """;

        try
        {
            await using var session = driver.AsyncSession(ConfigureSession());
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher).ConfigureAwait(false);
                var record = await cursor.SingleAsync(cancellationToken).ConfigureAwait(false);
                var analyticsData = record["analytics"].As<IDictionary<string, object>>();
                
                return MapToTransactionAnalytics(analyticsData);
            }, ConfigureTransaction("get_analytics"), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve transaction analytics");
            return await GetBasicAnalyticsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<IDictionary<string, object>> ExecuteQueryAsync(
        string cypher,
        object? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var session = driver.AsyncSession(ConfigureSession());
        
        await foreach (var record in ExecuteQueryInternalAsync(session, cypher, parameters, cancellationToken))
        {
            yield return record;
        }
    }

    public async IAsyncEnumerable<GraphStatistic> GetGraphStatisticsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string nodeStatsQuery = """
            CALL db.labels() YIELD label
            CALL {
                WITH label
                MATCH (n) WHERE label IN labels(n)
                RETURN count(n) as count
            }
            RETURN label, count
            """;

        const string relationshipStatsQuery = """
            CALL db.relationshipTypes() YIELD relationshipType
            CALL {
                WITH relationshipType
                MATCH ()-[r]->() WHERE type(r) = relationshipType
                RETURN count(r) as count
            }
            RETURN relationshipType, count
            """;

        await using var session = driver.AsyncSession(ConfigureSession());

        // Stream node statistics
        await foreach (var nodeRecord in ExecuteQueryInternalAsync(session, nodeStatsQuery, null, cancellationToken))
        {
            yield return new GraphStatistic(
                "Node",
                nodeRecord["label"]?.ToString() ?? "Unknown",
                Convert.ToInt64(nodeRecord["count"] ?? 0));
        }

        // Stream relationship statistics  
        await foreach (var relRecord in ExecuteQueryInternalAsync(session, relationshipStatsQuery, null, cancellationToken))
        {
            yield return new GraphStatistic(
                "Relationship", 
                relRecord["relationshipType"]?.ToString() ?? "Unknown",
                Convert.ToInt64(relRecord["count"] ?? 0));
        }
    }

    // Private helper methods using modern C# patterns

    private async IAsyncEnumerable<IDictionary<string, object>> ExecuteQueryInternalAsync(
        IAsyncSession session,
        string cypher,
        object? parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var record in session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(cypher, parameters).ConfigureAwait(false);
            var records = new List<IDictionary<string, object>>();
            
            await foreach (var record in cursor.ToAsyncEnumerable(cancellationToken))
            {
                records.Add(record.Keys.ToDictionary(key => key, key => record[key].As<object>()));
            }
            
            return records.ToAsyncEnumerable();
        }, ConfigureTransaction("custom_query"), cancellationToken).ConfigureAwait(false))
        {
            yield return record;
        }
    }

    private async ValueTask<string> UpsertSingleTransactionInSession(
        IAsyncSession session,
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        const string cypher = """
            MERGE (transaction:Transaction { id: $transactionId })
            ON CREATE SET
                transaction.amount = $amount,
                transaction.description = $description,
                transaction.status = $status,
                transaction.createdAt = datetime()
            ON MATCH SET
                transaction.amount = $amount,
                transaction.description = $description,
                transaction.status = $status,
                transaction.updatedAt = datetime()
            RETURN transaction.id AS transactionId
            """;

        var parameters = new
        {
            transactionId = transaction.Id,
            amount = (double)transaction.Amount,
            description = transaction.Description ?? "",
            status = transaction.Status.ToString()
        };

        return await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(cypher, parameters).ConfigureAwait(false);
            var record = await cursor.SingleAsync(cancellationToken).ConfigureAwait(false);
            return record["transactionId"].As<string>();
        }, ConfigureTransaction("batch_upsert"), cancellationToken).ConfigureAwait(false);
    }

    private static TransactionAnalytics MapToTransactionAnalytics(IDictionary<string, object> data) => new()
    {
        TotalTransactions = Convert.ToInt64(data.GetValueOrDefault("totalTransactions", 0L)),
        TotalAmount = Convert.ToDecimal(data.GetValueOrDefault("totalAmount", 0.0)),
        AverageAmount = Convert.ToDecimal(data.GetValueOrDefault("averageAmount", 0.0)),
        MinAmount = Convert.ToDecimal(data.GetValueOrDefault("minAmount", 0.0)),
        MaxAmount = Convert.ToDecimal(data.GetValueOrDefault("maxAmount", 0.0)),
        Categories = data.GetValueOrDefault("categories", new List<object>()) switch
        {
            IEnumerable<object> categories => categories.Select(c => c?.ToString() ?? "").ToArray(),
            _ => []
        },
        UniqueCategories = Convert.ToInt32(data.GetValueOrDefault("uniqueCategories", 0)),
        DateRange = MapToDateRange(data.GetValueOrDefault("dateRange", new Dictionary<string, object>())),
        Relationships = MapToRelationshipStats(data.GetValueOrDefault("relationships", new Dictionary<string, object>())),
        WeekendTransactions = Convert.ToInt64(data.GetValueOrDefault("weekendTransactions", 0L)),
        TopCategories = data.GetValueOrDefault("topCategories", new List<object>()) switch
        {
            IEnumerable<object> categories => categories
                .OfType<IDictionary<string, object>>()
                .Select(c => new CategoryBreakdown(
                    c.GetValueOrDefault("category", "Unknown")?.ToString() ?? "Unknown",
                    Convert.ToInt64(c.GetValueOrDefault("count", 0L))))
                .ToArray(),
            _ => []
        }
    };

    private static DateRange MapToDateRange(object? data) => data switch
    {
        IDictionary<string, object> dict => new DateRange(
            DateTime.TryParse(dict.GetValueOrDefault("earliest")?.ToString(), out var earliest) ? earliest : null,
            DateTime.TryParse(dict.GetValueOrDefault("latest")?.ToString(), out var latest) ? latest : null,
            Convert.ToInt32(dict.GetValueOrDefault("uniqueDays", 0)),
            Convert.ToInt32(dict.GetValueOrDefault("uniqueMonths", 0)),
            Convert.ToInt32(dict.GetValueOrDefault("uniqueYears", 0))),
        _ => new DateRange(null, null, 0, 0, 0)
    };

    private static RelationshipStats MapToRelationshipStats(object? data) => data switch
    {
        IDictionary<string, object> dict => new RelationshipStats(
            Convert.ToInt64(dict.GetValueOrDefault("categorySimilarities", 0L)),
            Convert.ToInt64(dict.GetValueOrDefault("amountSimilarities", 0L))),
        _ => new RelationshipStats(0, 0)
    };

    private async ValueTask<TransactionAnalytics> GetBasicAnalyticsAsync(CancellationToken cancellationToken)
    {
        const string basicCypher = """
            MATCH (t:Transaction)
            RETURN count(t) as totalTransactions,
                   sum(t.amount) as totalAmount,
                   avg(t.amount) as averageAmount,
                   min(t.amount) as minAmount,
                   max(t.amount) as maxAmount
            """;

        try
        {
            await using var session = driver.AsyncSession(ConfigureSession());
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(basicCypher).ConfigureAwait(false);
                var record = await cursor.SingleAsync(cancellationToken).ConfigureAwait(false);
                
                return new TransactionAnalytics
                {
                    TotalTransactions = record["totalTransactions"].As<long>(),
                    TotalAmount = Convert.ToDecimal(record["totalAmount"].As<double?>() ?? 0.0),
                    AverageAmount = Convert.ToDecimal(record["averageAmount"].As<double?>() ?? 0.0),
                    MinAmount = Convert.ToDecimal(record["minAmount"].As<double?>() ?? 0.0),
                    MaxAmount = Convert.ToDecimal(record["maxAmount"].As<double?>() ?? 0.0),
                    Categories = [],
                    UniqueCategories = 0,
                    DateRange = new DateRange(null, null, 0, 0, 0),
                    Relationships = new RelationshipStats(0, 0),
                    WeekendTransactions = 0,
                    TopCategories = []
                };
            }, ConfigureTransaction("basic_analytics"), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve basic analytics");
            return new TransactionAnalytics
            {
                TotalTransactions = 0,
                TotalAmount = 0,
                AverageAmount = 0,
                MinAmount = 0,
                MaxAmount = 0,
                Categories = [],
                UniqueCategories = 0,
                DateRange = new DateRange(null, null, 0, 0, 0),
                Relationships = new RelationshipStats(0, 0),
                WeekendTransactions = 0,
                TopCategories = []
            };
        }
    }

    private Action<SessionConfigBuilder> ConfigureSession(AccessMode accessMode = AccessMode.Read) =>
        builder => builder
            .WithDatabase(_settings.Database)
            .WithDefaultAccessMode(accessMode);

    private static Action<TransactionConfigBuilder> ConfigureTransaction(string operation, object? metadata = null) =>
        builder => builder
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithMetadata(new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["timestamp"] = DateTime.UtcNow,
                ["metadata"] = metadata ?? "none"
            });

    private static string GetDatabaseVersion() => $"v{DateTime.UtcNow:yyyy.MM.dd}";

    private static string GenerateTransactionHash(Transaction transaction)
    {
        var content = $"{transaction.Id}_{transaction.Amount}_{transaction.Description}_{transaction.Date:yyyy-MM-dd}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash)[..16]; // First 16 characters
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (driver != null)
            {
                await driver.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
            logger.LogDebug("Neo4j driver disposed");
        }
    }
}