using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public record Subcategory
{
    [MappingSource("name")]
    public required string Name { get; init; }

    [MappingSource("definition")]
    public required string Definition { get; init; }
}

public class Maincategory
{
    [MappingSource("name")]
    public required string Name { get; init; }

    [MappingSource("definition")]
    public required string Definition { get; init; }

    [MappingSource("subcategories")]
    public List<Subcategory> Subcategories { get; init; } = [];
}
