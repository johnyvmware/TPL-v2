using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Settings;

public record PromptsOptions
{
    [Required]
    public required string Path { get; init; }

    [Required]
    public required string CategorizerDeveloperMessage { get; init; }

    [Required]
    public required string CategorizerUserMessage { get; init; }
}
