namespace TransactionProcessingSystem.Configuration;

public record AppSettings
{
    public required OpenAISettings OpenAI { get; init; }
    public required MicrosoftGraphSettings MicrosoftGraph { get; init; }
    public required TransactionApiSettings TransactionApi { get; init; }
    public required ExportSettings Export { get; init; }
    public required PipelineSettings Pipeline { get; init; }
}

public record OpenAISettings
{
    public required string ApiKey { get; init; }
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxTokens { get; init; } = 200;
    public double Temperature { get; init; } = 0.1;
    public bool UseJsonSchema { get; init; } = true;
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