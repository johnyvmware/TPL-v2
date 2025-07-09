namespace TransactionProcessingSystem.Configuration;

public record AppSettings
{
    public required OpenAISettings OpenAI { get; init; }
    public required MicrosoftGraphSettings MicrosoftGraph { get; init; }
    public required TransactionApiSettings TransactionApi { get; init; }
    public required ExportSettings Export { get; init; }
    public required PipelineSettings Pipeline { get; init; }
    public required Neo4jSettings Neo4j { get; init; }
}

public record OpenAISettings
{
    public required string ApiKey { get; init; }
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxTokens { get; init; } = 200;
    public double Temperature { get; init; } = 0.1;
}

public record MicrosoftGraphSettings
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string TenantId { get; init; }
    public int EmailSearchDays { get; init; } = 2;
}

public record TransactionApiSettings
{
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
}

public record ExportSettings
{
    public required string OutputDirectory { get; init; }
    public string FileNameFormat { get; init; } = "transactions_{0:yyyyMMdd_HHmmss}.csv";
    public int BufferSize { get; init; } = 100;
}

public record PipelineSettings
{
    public int BoundedCapacity { get; init; } = 100;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public int TimeoutMinutes { get; init; } = 10;
}

public record Neo4jSettings
{
    public string ConnectionUri { get; init; } = "neo4j+s://demo.neo4jlabs.com";
    public string Username { get; init; } = "demo";
    public string Password { get; init; } = "demo";
    public string? Database { get; init; } = "neo4j";
    public int MaxConnectionPoolSize { get; init; } = 50;
    public int ConnectionTimeoutSeconds { get; init; } = 30;
    public int MaxTransactionRetryTimeSeconds { get; init; } = 30;
    public bool EnableMetrics { get; init; } = true;
}