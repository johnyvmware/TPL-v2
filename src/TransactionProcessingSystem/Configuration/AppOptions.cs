using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

public record LlmOptions
{
    public const string SectionName = "Llm";

    [Required]
    [ValidateObjectMembers]
    public required OpenAIOptions OpenAI { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required StructuredOutputsOptions StructuredOutputs { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required PromptsOptions Prompts { get; init; }
}
