using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

public record StructuredOutputsOptions
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string Categorizer { get; init; }
}
