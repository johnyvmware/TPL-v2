using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration;

public record CategoriesOptions
{
    public const string SectionName = "Categories";

    [Required]
    public required string Path { get; init; }
}
