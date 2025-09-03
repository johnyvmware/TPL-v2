using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

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
