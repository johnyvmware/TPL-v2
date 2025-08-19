using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public sealed class Exporter : IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly Neo4jOptions _settings;
    private readonly Neo4jSecrets _secrets;
    private readonly ILogger<Exporter> _logger;

    public Exporter(Neo4jOptions settings, Neo4jSecrets secrets, ILogger<Exporter> logger)
    {
        _settings = settings;
        _secrets = secrets;
        _logger = logger;

        _driver = GraphDatabase.Driver(
            uri: _secrets.Uri,
            authToken: AuthTokens.Basic(_secrets.User, _secrets.Password),
            action: config =>
            {
                config.WithMaxConnectionPoolSize(_settings.MaxConnectionPoolSize)
                      .WithConnectionTimeout(TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds))
                      .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(_settings.MaxTransactionRetryTimeSeconds));
            });
    }

    public Task VerifyConnectionAsync()
    {
        return _driver.VerifyConnectivityAsync();
    }

    public async Task ExportAsync(Transaction transaction)
    {
        var result = await _driver.ExecutableQuery(@"
            CREATE (a:Person {name: $name})
            CREATE (b:Person {name: $friendName})
            CREATE (a)-[:KNOWS]->(b)
            ")
            .WithParameters(new { name = "Alice", friendName = "David" })
            .WithConfig(new QueryConfig(database: _settings.Database))
            .ExecuteAsync();
    }

    public async Task CreateGraphAsync()
    {
        var result = await _driver.ExecutableQuery(@"
                CREATE (a:Person {name: $name})
                CREATE (b:Person {name: $friendName})
                CREATE (a)-[:KNOWS]->(b)
                ")
                .WithParameters(new { name = "Alice", friendName = "David" })
                .WithConfig(new QueryConfig(database: _settings.Database))
                .ExecuteAsync();

        var summary = result.Summary;
    }

    public async Task QueryGraphAsync()
    {
        var result = await _driver.ExecutableQuery(@"
            MATCH (p:Person)-[:KNOWS]->(:Person)
            RETURN p.name AS name
            ")
            .WithConfig(new QueryConfig(database: _settings.Database))
            .ExecuteAsync();

        // Loop through results and print people's name
        foreach (var record in result.Result) {
            Console.WriteLine(record.Get<string>("name"));
        }

        // Summary information
        var summary = result.Summary;
        Console.WriteLine($"The query `{summary.Query.Text}` returned {result.Result.Count()} results in {summary.ResultAvailableAfter.Milliseconds} ms.");
    }

    public ValueTask DisposeAsync()
    {
        return _driver.DisposeAsync();
    }
}