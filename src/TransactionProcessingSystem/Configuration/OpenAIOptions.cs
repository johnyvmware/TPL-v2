using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

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
