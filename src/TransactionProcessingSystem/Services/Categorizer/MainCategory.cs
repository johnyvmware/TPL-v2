using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

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
