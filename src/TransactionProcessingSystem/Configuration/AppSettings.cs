using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Application settings containing non-secret configuration values.
/// Secrets are handled separately via User Secrets (dev) and Environment Variables (prod).
/// </summary>
public record AppSettings
{
    public required LlmSettings LlmSettings { get; init; }

    public required MicrosoftGraphSettings MicrosoftGraph { get; init; }

    public required ExportSettings Export { get; init; }

    public required PipelineSettings Pipeline { get; init; }

    public required Neo4jSettings Neo4j { get; init; }

    public required TransactionFetcherSettings TransactionFetcher { get; init; }
}

public record LlmSettings
{
    [Required]
    public required OpenAI OpenAI { get; init; }

    [Required]
    public required StructuredOutputs StructuredOutputs { get; init; }

    [Required]
    public required Prompts Prompts { get; init; }
}

public record OpenAI
{
    [Required]
    public required string Model { get; init; }

    [Required]
    [Range(1, 4000)]
    public required int MaxTokens { get; init; }

    [Required]
    [Range(0, 2)]
    public required float Temperature { get; init; }
}

public record Prompts
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string CategorizerDeveloperMessage { get; init; }
}

public record StructuredOutputs
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string Categorizer { get; init; }
}

/// <summary>
/// Microsoft Graph configuration.
/// </summary>
public record MicrosoftGraphSettings
{
    [Required]
    [Range(1, 365)]
    public required int EmailSearchDays { get; init; }
}

/// <summary>
/// Export configuration settings.
/// </summary>
public record ExportSettings
{
    [Required]
    [RegularExpression(@"^[^<>:""/\\|?*\r\n]+([\\/][^<>:""/\\|?*\r\n]+)*$", ErrorMessage = "OutputDirectory must be a valid path (absolute or relative).")]
    public required string OutputDirectory { get; init; }

    [Required]
    [RegularExpression(@".*\{0\}.*", ErrorMessage = "FileNameFormat must contain the {0} placeholder for timestamp.")]
    public required string FileNameFormat { get; init; }

    [Required]
    [Range(1, 1024 * 1024)]
    public required int BufferSize { get; init; }
}

/// <summary>
/// Pipeline performance settings.
/// </summary>
public record PipelineSettings
{
    /// <summary>
    /// The maximum number of items allowed in the pipeline's input buffer at any time.
    /// Used to limit memory usage and control backpressure in TPL Dataflow pipelines.
    /// </summary>
    [Required]
    [Range(1, 100)]
    public required int InputBufferCapacity { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public required int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    [Required]
    [Range(0, 2, MinimumIsExclusive = true)]
    public required int TimeoutMinutes { get; init; }
}

/// <summary>
/// Configuration settings for Neo4j database connection and behavior.
/// </summary>
public record Neo4jSettings
{
    /// <summary>
    /// Target database name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Database { get; init; } = "neo4j";

    /// <summary>
    /// Maximum number of connections in the pool.
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int MaxConnectionPoolSize { get; init; }

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Required]
    [Range(1, 60)]
    public int ConnectionTimeoutSeconds { get; init; }

    /// <summary>
    /// Maximum retry time for transactions in seconds.
    /// </summary>
    [Required]
    [Range(1, 15)]
    public int MaxTransactionRetryTimeSeconds { get; init; }
}

public record TransactionFetcherSettings
{
    [Required]
    public required string InputDirectory { get; init; }

    [Required]
    public required string Encoding { get; init; }
}