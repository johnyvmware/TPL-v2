using Neo4j.Driver;
using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public class CategoryProviderV2(IDatabaseService databaseService)
{
    private readonly List<MainCategory> _categories = [];
    public IReadOnlyList<MainCategory> Categories => _categories;

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

    public IEnumerable<SubCategory> GetSubCategories(string mainCategory)
    {
        return _categories.FirstOrDefault(c => c.Name == mainCategory)?.Subcategories ?? [];
    }

    public IEnumerable<MainCategory> GetMainCategories()
    {
        return _categories;
    }

    public bool IsValidMainCategory(string mainCategory)
    {
        return _categories.Any(c => c.Name == mainCategory);
    }

    public bool IsValidSubCategory(string subCategory)
    {
        return _categories.SelectMany(c => c.Subcategories).Any(sc => sc.Name == subCategory);
    }

    public bool IsValidCombination(string mainCategory, string subCategory)
    {
        return _categories.FirstOrDefault(c => c.Name == mainCategory)?.Subcategories.Select(sc => sc.Name)
            .Contains(subCategory) ?? false;
    }

    private void MapToCategories(EagerResult<IReadOnlyList<IRecord>> eagerResult)
    {
        foreach (var record in eagerResult.Result)
        {
            var dto = record.AsObject<MainCategory>();
            _categories.Add(dto);
        }
    }
}
