using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

public record FetcherOptions
{
    [Required]
    public required string InputDirectory { get; init; }

    [Required]
    public required string Encoding { get; init; }
}