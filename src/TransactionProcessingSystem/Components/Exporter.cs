using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Components;

public sealed class Exporter(IDatabaseService databaseService)
{
    private readonly IDatabaseService _databaseService = databaseService;

    public async Task ExportAsync(RawTransaction transaction)
    {
        var result = await _databaseService.ExecutableQuery(@"
            CREATE (a:Person {name: $name})
            CREATE (b:Person {name: $friendName})
            CREATE (a)-[:KNOWS]->(b)
            ")
            .WithParameters(new { name = "Alice", friendName = "David" })
            .ExecuteAsync();
    }

    public async Task CreateGraphAsync()
    {
        var result = await _databaseService.ExecutableQuery(@"
                CREATE (a:Person {name: $name})
                CREATE (b:Person {name: $friendName})
                CREATE (a)-[:KNOWS]->(b)
                ")
                .WithParameters(new { name = "Alice", friendName = "David" })
                .ExecuteAsync();

        var summary = result.Summary;
    }
}