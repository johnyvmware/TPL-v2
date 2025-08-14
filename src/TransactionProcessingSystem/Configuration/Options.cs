using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

public record LlmOptions
{
    public const string SectionName = "Llm";

    [Required, ValidateObjectMembers]
    public required OpenAIOptions OpenAI { get; init; }

    [Required, ValidateObjectMembers]
    public required StructuredOutputsOptions StructuredOutputs { get; init; }

    [Required, ValidateObjectMembers]
    public required PromptsOptions Prompts { get; init; }
}

public record CategoriesOptions
{
    public const string SectionName = "Categories";

    [Required]
    public required string Path { get; init; }
}

public record OpenAIOptions
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

public record PromptsOptions
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string CategorizerDeveloperMessage { get; init; }
}

public record StructuredOutputsOptions
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string Categorizer { get; init; }
}

/// <summary>
/// Microsoft Graph configuration.
/// </summary>
public record MicrosoftGraphOptions
{
    [Required]
    [Range(1, 365)]
    public required int EmailSearchDays { get; init; }
}

/// <summary>
/// Export configuration settings.
/// </summary>
public record ExportOptions
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
public record PipelineOptions
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
public record Neo4jOptions
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

public record FetcherOptions
{
    [Required]
    public required string InputDirectory { get; init; }

    [Required]
    public required string Encoding { get; init; }
}