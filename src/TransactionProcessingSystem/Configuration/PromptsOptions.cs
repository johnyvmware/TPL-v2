using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

public record PromptsOptions
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string CategorizerDeveloperMessage { get; init; }

    [Required]
    public required string CategorizerUserMessage { get; init; }
}
