namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Application settings containing non-secret configuration values.
/// Secrets are handled separately via User Secrets (dev) and Environment Variables (prod).
/// Neo4j configuration is handled separately to follow SRP.
/// </summary>
public record AppSettings
{
    public required OpenAISettings OpenAI { get; init; }
    public required MicrosoftGraphSettings MicrosoftGraph { get; init; }
    public required TransactionApiSettings TransactionApi { get; init; }
    public required ExportSettings Export { get; init; }
    public required PipelineSettings Pipeline { get; init; }
}

/// <summary>
/// OpenAI configuration - API key stored as secret
/// </summary>
public record OpenAISettings
{
    public string Model { get; init; } = "gpt-4o-mini";
    public int MaxTokens { get; init; } = 200;
    public double Temperature { get; init; } = 0.1;
}

/// <summary>
/// Microsoft Graph configuration - secrets stored separately
/// </summary>
public record MicrosoftGraphSettings
{
    public int EmailSearchDays { get; init; } = 2;
}

/// <summary>
/// Transaction API configuration - currently mock service with placeholders
/// </summary>
public record TransactionApiSettings
{
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
    public int BatchSize { get; init; } = 50;
    public bool EnableMockData { get; init; } = true;
}

/// <summary>
/// Export configuration settings
/// </summary>
public record ExportSettings
{
    public required string OutputDirectory { get; init; }
    public string FileNameFormat { get; init; } = "transactions_{0:yyyyMMdd_HHmmss}.csv";
    public int BufferSize { get; init; } = 100;
}

/// <summary>
/// Pipeline performance settings
/// </summary>
public record PipelineSettings
{
    public int BoundedCapacity { get; init; } = 100;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public int TimeoutMinutes { get; init; } = 10;
}