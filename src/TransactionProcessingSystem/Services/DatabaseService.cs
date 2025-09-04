using Neo4j.Driver;
using TransactionProcessingSystem.Configuration;

namespace TransactionProcessingSystem.Services;

public interface IDatabaseService : IAsyncDisposable
{
    IExecutableQuery<IRecord, IRecord> ExecutableQuery(string cypher);

    Task<EagerResult<IReadOnlyList<IRecord>>> ExecuteQueryAsync(string cypher);

    Task VerifyConnectionAsync();
}

public sealed class DatabaseService(Neo4jOptions settings, Neo4jSecrets secrets) : IDatabaseService
{
    private readonly IDriver _driver = GraphDatabase.Driver(
        uri: secrets.Uri,
        authToken: AuthTokens.Basic(secrets.User, secrets.Password),
        action: config =>
        {
            config
                .WithMaxConnectionPoolSize(settings.MaxConnectionPoolSize)
                .WithConnectionTimeout(TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds))
                .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(settings.MaxTransactionRetryTimeSeconds));
        });

    public ValueTask DisposeAsync()
    {
        return _driver.DisposeAsync();
    }

    public IExecutableQuery<IRecord, IRecord> ExecutableQuery(string cypher)
    {
        return _driver
            .ExecutableQuery(cypher)
            .WithConfig(new QueryConfig(database: settings.Database));
    }

    public Task<EagerResult<IReadOnlyList<IRecord>>> ExecuteQueryAsync(string cypher)
    {
        return _driver
            .ExecutableQuery(cypher)
            .WithConfig(new QueryConfig(database: settings.Database))
            .ExecuteAsync();
    }

    public Task VerifyConnectionAsync()
    {
        return _driver.VerifyConnectivityAsync();
    }
}
