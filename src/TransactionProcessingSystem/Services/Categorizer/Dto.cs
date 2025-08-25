using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public record CategoryInfo
{
    public required string Name { get; init; }

    public required string Definition { get; init; }
}

public record SubCategory
{
    [MappingSource("name")]
    public required string Name { get; init; }

    [MappingSource("definition")]
    public required string Definition { get; init; }

    public CategoryInfo ToCategoryInfo() => new() { Name = Name, Definition = Definition };
}

public class MainCategory(
    string name,
    string definition,
    [MappingSource("subcategories")] List<SubCategory> subcategories)
{
    public string Name { get; } = name;

    public string Definition { get; } = definition;

    [MappingSource("subcategories")]
    public IReadOnlyList<SubCategory> Subcategories { get; } = subcategories;

    public CategoryInfo ToCategoryInfo() => new() { Name = Name, Definition = Definition };

}