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
            MERGE (year:Year {value: $year})
            MERGE (month:Month {value: $month, name: $monthName, year: $year})
            MERGE (day:Day {value: $day, date: date($transactionDate), year: $year, month: $month})
            
            // Create relationships between date nodes
            MERGE (day)-[:IN_MONTH]->(month)
            MERGE (month)-[:IN_YEAR]->(year)
            
            // Create or update the transaction node
            MERGE (transaction:Transaction {id: $transactionId})
            SET transaction.amount = $amount,
                transaction.description = $description,
                transaction.cleanDescription = $cleanDescription,
                transaction.status = $status,
                transaction.lastUpdated = datetime(),
                transaction.hash = $transactionHash
            
            // Create relationships
            MERGE (transaction)-[:BELONGS_TO_CATEGORY]->(category)
            MERGE (transaction)-[:OCCURRED_ON]->(day)
            MERGE (transaction)-[:STORED_IN_VERSION]->(dbVersion)
            
            // Add amount-based relationships for analytics
            WITH transaction, category, day
            CALL {
                WITH transaction, category
                MATCH (otherTransaction:Transaction)-[:BELONGS_TO_CATEGORY]->(category)
                WHERE otherTransaction <> transaction 
                  AND abs(otherTransaction.amount - transaction.amount) <= $amountThreshold
                MERGE (transaction)-[:SIMILAR_AMOUNT {
                    difference: abs(otherTransaction.amount - transaction.amount),
                    createdAt: datetime()
                }]->(otherTransaction)
            }
            
            // Add temporal relationships (same day transactions)
            WITH transaction, day
            CALL {
                WITH transaction, day
                MATCH (otherTransaction:Transaction)-[:OCCURRED_ON]->(day)
                WHERE otherTransaction <> transaction
                MERGE (transaction)-[:SAME_DAY {createdAt: datetime()}]->(otherTransaction)
            }
            
            RETURN transaction.id as transactionId, 
                   category.name as categoryName,
                   day.date as transactionDate,
                   dbVersion.version as databaseVersion
            """;

        var transactionDate = transaction.Date;
        var parameters = new
        {
            transactionId = transaction.Id,
            amount = (double)transaction.Amount,
            description = transaction.Description,
            cleanDescription = transaction.CleanDescription ?? transaction.Description,
            category = transaction.Category ?? "Unknown",
            status = transaction.Status,
            transactionDate = transactionDate.ToString("yyyy-MM-dd"),
            year = transactionDate.Year,
            month = transactionDate.Month,
            monthName = transactionDate.ToString("MMMM"),
            day = transactionDate.Day,
            dbVersion = GetCurrentDatabaseVersion(),
            today = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            transactionHash = GenerateTransactionHash(transaction),
            amountThreshold = 10.0 // Similar amounts within $10
        };

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Write));
            
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                _logger.LogDebug("Upserting transaction {TransactionId} into graph database", transaction.Id);
                
                var cursor = await tx.RunAsync(cypher, parameters);
                var record = await cursor.SingleAsync();
                
                return new
                {
                    TransactionId = record["transactionId"].As<string>(),
                    CategoryName = record["categoryName"].As<string>(),
                    TransactionDate = record["transactionDate"].As<string>(),
                    DatabaseVersion = record["databaseVersion"].As<string>()
                };
            });

            _logger.LogDebug("Successfully upserted transaction {TransactionId} with category {Category} on {Date} in database version {Version}", 
                result.TransactionId, result.CategoryName, result.TransactionDate, result.DatabaseVersion);

            return result.TransactionId;
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
            // Basic transaction statistics
            MATCH (t:Transaction)
            WITH count(t) as totalTransactions,
                 sum(t.amount) as totalAmount,
                 avg(t.amount) as averageAmount,
                 min(t.amount) as minAmount,
                 max(t.amount) as maxAmount
            
            // Category analysis
            OPTIONAL MATCH (t:Transaction)-[:BELONGS_TO_CATEGORY]->(c:Category)
            WITH totalTransactions, totalAmount, averageAmount, minAmount, maxAmount,
                 count(DISTINCT c) as totalCategories,
                 collect(DISTINCT {name: c.name, count: size((c)<-[:BELONGS_TO_CATEGORY]-())}) as categoryBreakdown
            
            // Temporal analysis
            OPTIONAL MATCH (t:Transaction)-[:OCCURRED_ON]->(d:Day)-[:IN_MONTH]->(m:Month)-[:IN_YEAR]->(y:Year)
            WITH totalTransactions, totalAmount, averageAmount, minAmount, maxAmount,
                 totalCategories, categoryBreakdown,
                 count(DISTINCT y) as yearsSpan,
                 count(DISTINCT m) as monthsSpan,
                 count(DISTINCT d) as daysSpan
            
            // Relationship analysis
            OPTIONAL MATCH ()-[sim:SIMILAR_AMOUNT]->()
            OPTIONAL MATCH ()-[same:SAME_DAY]->()
            WITH totalTransactions, totalAmount, averageAmount, minAmount, maxAmount,
                 totalCategories, categoryBreakdown, yearsSpan, monthsSpan, daysSpan,
                 count(DISTINCT sim) as similarAmountRelationships,
                 count(DISTINCT same) as sameDayRelationships
            
            // Database version info
            OPTIONAL MATCH (dv:DatabaseVersion)
            
            RETURN {
                transactions: {
                    total: totalTransactions,
                    totalAmount: coalesce(totalAmount, 0.0),
                    averageAmount: round(coalesce(averageAmount, 0.0) * 100) / 100,
                    minAmount: coalesce(minAmount, 0.0),
                    maxAmount: coalesce(maxAmount, 0.0)
                },
                categories: {
                    total: totalCategories,
                    breakdown: categoryBreakdown
                },
                temporal: {
                    yearsSpan: yearsSpan,
                    monthsSpan: monthsSpan,
                    daysSpan: daysSpan
                },
                relationships: {
                    similarAmount: similarAmountRelationships,
                    sameDay: sameDayRelationships
                },
                database: {
                    version: collect(DISTINCT dv.version)[0],
                    generatedAt: datetime()
                }
            } as analytics
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
                
                var analytics = record["analytics"].As<IDictionary<string, object>>();
                return analytics;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enhanced transaction analytics");
            
            // Fallback to basic statistics if the enhanced query fails
            return await GetBasicAnalyticsAsync();
        }
    }

    private async Task<IDictionary<string, object>> GetBasicAnalyticsAsync()
    {
        const string basicCypher = """
            MATCH (t:Transaction)
            RETURN 
                count(t) as totalTransactions,
                sum(t.amount) as totalAmount,
                avg(t.amount) as averageAmount,
                min(t.amount) as minAmount,
                max(t.amount) as maxAmount
            """;

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Read));
            
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(basicCypher);
                var record = await cursor.SingleAsync();
                
                return new Dictionary<string, object>
                {
                    ["transactions"] = new Dictionary<string, object>
                    {
                        ["total"] = record["totalTransactions"].As<long>(),
                        ["totalAmount"] = record["totalAmount"].As<double?>() ?? 0.0,
                        ["averageAmount"] = Math.Round(record["averageAmount"].As<double?>() ?? 0.0, 2),
                        ["minAmount"] = record["minAmount"].As<double?>() ?? 0.0,
                        ["maxAmount"] = record["maxAmount"].As<double?>() ?? 0.0
                    },
                    ["categories"] = new Dictionary<string, object> { ["total"] = 0 },
                    ["relationships"] = new Dictionary<string, object> { ["similarAmount"] = 0, ["sameDay"] = 0 },
                    ["database"] = new Dictionary<string, object> { ["fallback"] = true }
                };
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get basic analytics");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Demonstrates the power of our graph database design with complex queries
    /// </summary>
    public async Task<IDictionary<string, object>> GetAdvancedTransactionPatternsAsync(CancellationToken cancellationToken = default)
    {
        const string cypher = """
            // Find spending patterns by category over time
            MATCH (t:Transaction)-[:BELONGS_TO_CATEGORY]->(c:Category),
                  (t)-[:OCCURRED_ON]->(d:Day)-[:IN_MONTH]->(m:Month)-[:IN_YEAR]->(y:Year)
            WITH c.name as category,
                 y.value as year,
                 m.value as month,
                 sum(t.amount) as monthlyTotal,
                 count(t) as transactionCount,
                 avg(t.amount) as avgTransactionAmount
            
            // Find categories with consistent spending patterns
            WITH category, year, month, monthlyTotal, transactionCount, avgTransactionAmount,
                 collect({month: month, total: monthlyTotal, count: transactionCount}) as monthlyData
            
            // Find similar amount clusters
            MATCH (t1:Transaction)-[sim:SIMILAR_AMOUNT]->(t2:Transaction)
            WITH category, year, monthlyData,
                 count(DISTINCT sim) as similarAmountConnections
            
            // Find temporal spending clusters (same day transactions)
            MATCH (t:Transaction)-[:SAME_DAY]->(other:Transaction)
            WHERE t <> other
            WITH category, year, monthlyData, similarAmountConnections,
                 count(DISTINCT t) as transactionsWithSameDayActivity
            
            // Database version and integrity info
            MATCH (dv:DatabaseVersion)
            
            RETURN {
                spendingPatterns: collect({
                    category: category,
                    year: year,
                    monthlyBreakdown: monthlyData,
                    similarAmountConnections: similarAmountConnections,
                    sameDayActivityTransactions: transactionsWithSameDayActivity
                }),
                graphIntegrity: {
                    databaseVersion: dv.version,
                    features: dv.features,
                    analysisDate: datetime(),
                    description: dv.description
                }
            } as patterns
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
                
                var patterns = record["patterns"].As<IDictionary<string, object>>();
                return patterns;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get advanced transaction patterns");
            return new Dictionary<string, object>
            {
                ["error"] = "Advanced pattern analysis unavailable",
                ["message"] = ex.Message
            };
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

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var constraints = new[]
        {
            "CREATE CONSTRAINT transaction_id_unique IF NOT EXISTS FOR (t:Transaction) REQUIRE t.id IS UNIQUE",
            "CREATE CONSTRAINT category_name_unique IF NOT EXISTS FOR (c:Category) REQUIRE c.name IS UNIQUE",
            "CREATE CONSTRAINT year_value_unique IF NOT EXISTS FOR (y:Year) REQUIRE y.value IS UNIQUE",
            "CREATE CONSTRAINT month_composite_unique IF NOT EXISTS FOR (m:Month) REQUIRE (m.value, m.year) IS UNIQUE",
            "CREATE CONSTRAINT day_date_unique IF NOT EXISTS FOR (d:Day) REQUIRE d.date IS UNIQUE",
            "CREATE CONSTRAINT db_version_unique IF NOT EXISTS FOR (dv:DatabaseVersion) REQUIRE dv.version IS UNIQUE"
        };

        var indexes = new[]
        {
            "CREATE INDEX transaction_amount_idx IF NOT EXISTS FOR (t:Transaction) ON (t.amount)",
            "CREATE INDEX transaction_status_idx IF NOT EXISTS FOR (t:Transaction) ON (t.status)",
            "CREATE INDEX transaction_date_idx IF NOT EXISTS FOR (t:Transaction) ON (t.lastUpdated)",
            "CREATE INDEX category_normalized_idx IF NOT EXISTS FOR (c:Category) ON (c.normalizedName)",
            "CREATE INDEX day_date_idx IF NOT EXISTS FOR (d:Day) ON (d.date)",
            "CREATE INDEX year_value_idx IF NOT EXISTS FOR (y:Year) ON (y.value)",
            "CREATE TEXT INDEX transaction_description_text_idx IF NOT EXISTS FOR (t:Transaction) ON (t.description)",
            "CREATE TEXT INDEX transaction_clean_description_text_idx IF NOT EXISTS FOR (t:Transaction) ON (t.cleanDescription)"
        };

        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_settings.Database)
                .WithDefaultAccessMode(AccessMode.Write));
            
            // Create constraints first
            foreach (var constraint in constraints)
            {
                try
                {
                    await session.ExecuteWriteAsync(async tx =>
                    {
                        await tx.RunAsync(constraint);
                    });
                    _logger.LogDebug("Created constraint: {Constraint}", constraint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create constraint: {Constraint}", constraint);
                }
            }

            // Create indexes
            foreach (var index in indexes)
            {
                try
                {
                    await session.ExecuteWriteAsync(async tx =>
                    {
                        await tx.RunAsync(index);
                    });
                    _logger.LogDebug("Created index: {Index}", index);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create index: {Index}", index);
                }
            }

            // Initialize database version
            await CreateDatabaseVersionAsync(session);

            _logger.LogInformation("Neo4j database schema initialized successfully with {ConstraintCount} constraints and {IndexCount} indexes", 
                constraints.Length, indexes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Neo4j database schema");
            throw;
        }
    }

    private async Task CreateDatabaseVersionAsync(IAsyncSession session)
    {
        const string cypher = """
            MERGE (dv:DatabaseVersion {version: $version})
            SET dv.createdDate = date(),
                dv.description = $description,
                dv.features = $features
            RETURN dv.version as version
            """;

        var currentVersion = GetCurrentDatabaseVersion();
        var parameters = new
        {
            version = currentVersion,
            description = "Transaction Processing System Graph Database",
            features = new[] { "Hierarchical_Dates", "Category_Relationships", "Amount_Similarity", "Temporal_Links", "Version_Tracking" }
        };

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(cypher, parameters);
        });

        _logger.LogInformation("Database version {Version} initialized", currentVersion);
    }

    private string GetCurrentDatabaseVersion()
    {
        // Version based on current date and feature set
        var version = $"v2.0.{DateTime.UtcNow:yyyyMMdd}";
        return version;
    }

    private string GenerateTransactionHash(Transaction transaction)
    {
        // Generate a hash for duplicate detection and integrity checking
        var hashInput = $"{transaction.Id}|{transaction.Amount}|{transaction.Date:yyyy-MM-dd}|{transaction.Description}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes)[..12]; // First 12 characters for readability
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