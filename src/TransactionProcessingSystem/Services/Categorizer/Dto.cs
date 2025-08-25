using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public record SubCategory
{
    [MappingSource("name")]
    public required string Name { get; init; }

    [MappingSource("definition")]
    public required string Definition { get; init; }
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
}
