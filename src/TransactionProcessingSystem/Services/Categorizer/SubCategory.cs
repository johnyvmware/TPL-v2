using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public record SubCategory
{
    [MappingSource("name")]
    public required string Name { get; init; }

    [MappingSource("definition")]
    public required string Definition { get; init; }

    public CategoryInfo ToCategoryInfo() => new() { Name = Name, Definition = Definition };
}
