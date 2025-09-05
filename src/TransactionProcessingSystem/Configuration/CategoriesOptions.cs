using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

public record CategoriesOptions
{
    public const string SectionName = "Categories";

    [Required]
    public required string Path { get; init; }
}
