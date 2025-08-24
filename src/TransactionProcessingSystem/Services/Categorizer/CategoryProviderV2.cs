using System;
using Microsoft.Graph.Security.Labels.Categories.Item.Subcategories;
using Neo4j.Driver.Mapping;

namespace TransactionProcessingSystem.Services.Categorizer;

public class CategoryProviderV2(IDatabaseService databaseService)
{
    private IReadOnlyList<Maincategory> _categories = [];
    public IReadOnlyList<Maincategory> Categories => _categories;

    public async Task LoadAsync()
    {
        var cypher = """
            MATCH (main:Category)
            WHERE NOT (main)-[:CHILD_OF]->(:Category)
            OPTIONAL MATCH (sub:Category)-[:CHILD_OF]->(main)
            WITH main, collect(DISTINCT {name: sub.name, definition: sub.definition}) AS subcategories
            RETURN main.name AS name, main.definition AS definition, subcategories
            """;

        var cursor = await databaseService
            .ExecutableQuery(cypher)
            .ExecuteAsync();

        var list = new List<Maincategory>();

        foreach (var record in cursor.Result)
        {
            var dto = record.AsObject<Maincategory>();
            list.Add(dto);
        }

        _categories = list;
    }

    public IEnumerable<Subcategory> GetSubcategories(string mainCategory)
    {
        return _categories.FirstOrDefault(c => c.Name == mainCategory)?.Subcategories ?? [];
    }

    public IEnumerable<Maincategory> GetMainCategories()
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
}
