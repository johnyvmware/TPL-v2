using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration.Secrets;

public record OpenAISecrets
{
    public const string SectionName = "OpenAI";

    [Required]
    public required string ApiKey { get; init; }
}
