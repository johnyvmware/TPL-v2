using System;

namespace TransactionProcessingSystem.Services.Categorizer;

public class CategoryProviderV2(IDatabaseService databaseService)
{
    public async Task LoadAsync()
    {
        var result = await databaseService.ExecutableQuery(@"
            MATCH (mainCategory:Category)<-[:CHILD_OF]-(subCategory:Category)
            WITH mainCategory, collect(subCategory) AS subcategories
            RETURN mainCategory.name AS mainCategoryName, 
                mainCategory.definition AS mainCategoryDefinition,
                [sub IN subcategories | {name: sub.name, definition: sub.definition}] AS subcategories
            ORDER BY mainCategoryName
            ")
            .ExecuteAsync();

        // Loop through results and print people's name
        foreach (var record in result.Result) {
            Console.WriteLine(record.Get<string>("name"));
        }

        // Summary information
        var summary = result.Summary;
        Console.WriteLine($"The query `{summary.Query.Text}` returned {result.Result.Count()} results in {summary.ResultAvailableAfter.Milliseconds} ms.");
 
    }
}
