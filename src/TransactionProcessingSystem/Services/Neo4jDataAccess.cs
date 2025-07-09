using Neo4j.Driver;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using System.Text.Json;

namespace TransactionProcessingSystem.Services;

/// <summary>
/// Modern Neo4j data access implementation following official .NET driver best practices
/// Implements patterns from: https://neo4j.com/docs/dotnet-manual/current/
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
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying Neo4j connectivity");

            // Use async session with modern pattern
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));

            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1 AS test", null);
                var record = await cursor.SingleAsync();
                return record["test"].As<int>();
            }, txConfig => txConfig
                .WithTimeout(TimeSpan.FromSeconds(5))
                .WithMetadata(new Dictionary<string, object> { ["operation"] = "connectivity_check" }));

            _logger.LogInformation("Neo4j connectivity verified successfully");
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo4j connectivity verification failed");
            return false;
        }
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var constraints = new[]
        {
            "CREATE CONSTRAINT transaction_id_unique IF NOT EXISTS FOR (t:Transaction) REQUIRE t.id IS UNIQUE",
            "CREATE CONSTRAINT category_name_unique IF NOT EXISTS FOR (c:Category) REQUIRE c.name IS UNIQUE",
            "CREATE CONSTRAINT year_value_unique IF NOT EXISTS FOR (y:Year) REQUIRE y.value IS UNIQUE",
            "CREATE CONSTRAINT month_composite_unique IF NOT EXISTS FOR (m:Month) REQUIRE (m.value, m.year) IS UNIQUE",
            "CREATE CONSTRAINT day_date_unique IF NOT EXISTS FOR (d:Day) REQUIRE d.date IS UNIQUE",
            "CREATE CONSTRAINT db_version_unique IF NOT EXISTS FOR (v:DatabaseVersion) REQUIRE v.version IS UNIQUE"
        };

        var indexes = new[]
        {
            "CREATE INDEX transaction_amount_idx IF NOT EXISTS FOR (t:Transaction) ON (t.amount)",
            "CREATE INDEX transaction_date_idx IF NOT EXISTS FOR (t:Transaction) ON (t.date)",
            "CREATE INDEX transaction_hash_idx IF NOT EXISTS FOR (t:Transaction) ON (t.contentHash)",
            "CREATE INDEX category_normalized_idx IF NOT EXISTS FOR (c:Category) ON (c.normalizedName)",
            "CREATE FULLTEXT INDEX transaction_description_fulltext IF NOT EXISTS FOR (t:Transaction) ON EACH [t.description, t.cleanDescription]"
        };

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Write));

            await session.ExecuteWriteAsync(async tx =>
            {
                // Create constraints first
                foreach (var constraint in constraints)
                {
                    try
                    {
                        await tx.RunAsync(constraint, null);
                        _logger.LogDebug("Created constraint: {Constraint}", constraint);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Constraint already exists or failed: {Constraint}", constraint);
                    }
                }

                // Create indexes
                foreach (var index in indexes)
                {
                    try
                    {
                        await tx.RunAsync(index, null);
                        _logger.LogDebug("Created index: {Index}", index);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Index already exists or failed: {Index}", index);
                    }
                }

                return Task.CompletedTask;
            }, txConfig => txConfig
                .WithTimeout(TimeSpan.FromMinutes(2))
                .WithMetadata(new Dictionary<string, object>
                {
                    ["operation"] = "schema_initialization",
                    ["version"] = GetDatabaseVersion()
                }));

            _logger.LogInformation("Neo4j database schema initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Neo4j database schema");
            throw;
        }
    }

    public async Task<string> UpsertTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
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
            
            // Create or get the date nodes (hierarchical: Year -> Month -> Day)
            MERGE (year:Year {
                value: $year
            })
            MERGE (month:Month {
                value: $month,
                year: $year
            })
            MERGE (day:Day {
                date: date($transactionDate),
                dayOfWeek: $dayOfWeek,
                dayOfMonth: $dayOfMonth,
                isWeekend: $isWeekend
            })
            
            // Create relationships between date nodes
            MERGE (year)-[:HAS_MONTH]->(month)
            MERGE (month)-[:HAS_DAY]->(day)
            
            // Create or update the transaction node with enhanced properties
            MERGE (transaction:Transaction {
                id: $transactionId
            })
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
            
            // Create similarity relationships (same category transactions)
            WITH transaction, category
            MATCH (other:Transaction)-[:BELONGS_TO_CATEGORY]->(category)
            WHERE other.id <> transaction.id
            MERGE (transaction)-[:SAME_CATEGORY]->(other)
            
            // Create amount similarity relationships (within 10% or $10)
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
            isWeekend = transactionDate.DayOfWeek == DayOfWeek.Saturday || transactionDate.DayOfWeek == DayOfWeek.Sunday,
            today = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            dbVersion = GetDatabaseVersion(),
            contentHash = GenerateTransactionHash(transaction)
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
            }, txConfig => txConfig
                .WithTimeout(TimeSpan.FromSeconds(30))
                .WithMetadata(new Dictionary<string, object>
                {
                    ["operation"] = "upsert_transaction",
                    ["transactionId"] = transaction.Id
                }));

            _logger.LogDebug("Successfully upserted transaction {TransactionId} with relationships", transaction.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert transaction {TransactionId}", transaction.Id);
            throw;
        }
    }

    public async Task<IDictionary<string, object>> GetTransactionAnalyticsAsync(CancellationToken cancellationToken = default)
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
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));

            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, null);
                var record = await cursor.SingleAsync();
                var analytics = record["analytics"].As<IDictionary<string, object>>();

                _logger.LogDebug("Retrieved transaction analytics with {Count} total transactions",
                    analytics.TryGetValue("totalTransactions", out var count) ? count : 0);

                return analytics;
            }, txConfig => txConfig
                .WithTimeout(TimeSpan.FromSeconds(30))
                .WithMetadata(new Dictionary<string, object>
                {
                    ["operation"] = "get_analytics"
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve transaction analytics");
            return await GetBasicAnalyticsAsync();
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
                return records.Select(record => record.Keys.ToDictionary(key => key, key => record[key].As<object>())).ToList();
            }, txConfig => txConfig
                .WithTimeout(TimeSpan.FromMinutes(5))
                .WithMetadata(new Dictionary<string, object>
                {
                    ["operation"] = "custom_query",
                    ["query_preview"] = cypher.Substring(0, Math.Min(cypher.Length, 100))
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute custom query");
            throw;
        }
    }

    private async Task<IDictionary<string, object>> GetBasicAnalyticsAsync()
    {
        const string basicCypher = """
            MATCH (t:Transaction)
            RETURN {
                totalTransactions: count(t),
                totalAmount: sum(t.amount),
                averageAmount: avg(t.amount),
                minAmount: min(t.amount),
                maxAmount: max(t.amount)
            } AS analytics
            """;

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));

            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(basicCypher, null);
                var record = await cursor.SingleAsync();
                return record["analytics"].As<IDictionary<string, object>>();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve basic analytics");
            return new Dictionary<string, object>
            {
                ["totalTransactions"] = 0,
                ["totalAmount"] = 0.0,
                ["averageAmount"] = 0.0,
                ["minAmount"] = 0.0,
                ["maxAmount"] = 0.0,
                ["error"] = ex.Message
            };
        }
    }

    private string GetDatabaseVersion()
    {
        return $"v{DateTime.UtcNow:yyyy.MM.dd}";
    }

    private string GenerateTransactionHash(Transaction transaction)
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
            if (_driver != null)
            {
                await _driver.DisposeAsync();
            }
            _disposed = true;
            _logger.LogDebug("Neo4j driver disposed");
        }
    }
}