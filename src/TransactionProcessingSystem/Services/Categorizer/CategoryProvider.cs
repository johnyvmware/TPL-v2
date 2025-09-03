using Neo4j.Driver;
using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public class CategoryProvider(IDatabaseService databaseService)
{
    private readonly List<MainCategory> categories = [];

    public async Task LoadAsync()
    {
        var loadCategoriesCypher = """
            MATCH (main:Category:Main)-[:HAS_SUBCATEGORY]->(sub:Category:Sub)
            WITH main, collect({name: sub.name, definition: sub.definition}) AS subcategories
            RETURN main.name AS name, main.definition AS definition, subcategories
            """;

        EagerResult<IReadOnlyList<IRecord>> eagerResult = await databaseService.ExecuteQueryAsync(loadCategoriesCypher);
        MapToCategories(eagerResult);
    }

    public IReadOnlyList<CategoryInfo> GetSubCategoriesFor(string mainCategory)
    {
        return categories.FirstOrDefault(c => c.Name == mainCategory)?.Subcategories.Select(sc => sc.ToCategoryInfo()).ToList() ?? [];
    }

    public IReadOnlyList<CategoryInfo> GetSubCategories()
    {
        return categories.SelectMany(c => c.Subcategories).Select(sc => sc.ToCategoryInfo()).ToList() ?? [];
    }

    public IReadOnlyList<CategoryInfo> GetMainCategories()
    {
        return [.. categories.Select(c => c.ToCategoryInfo())];
    }

    private void MapToCategories(EagerResult<IReadOnlyList<IRecord>> eagerResult)
    {
        foreach (var record in eagerResult.Result)
        {
            var dto = record.AsObject<MainCategory>();
            categories.Add(dto);
        }
    }
}
