using OpenAI.Chat;
using OpenAI.Responses;
using TransactionProcessingSystem.Models.Categories;

namespace TransactionProcessingSystem.Tools;

public static class CategoryDefinitions
{
    public static readonly Dictionary<string, List<string>> Categories = new()
    {
        [nameof(Home)] = [.. Enum.GetNames<Home>()],
        [nameof(Daily)] = [.. Enum.GetNames<Daily>()],
        [nameof(Education)] = [.. Enum.GetNames<Education>()],
        [nameof(Transport)] = [.. Enum.GetNames<Transport>()],
        [nameof(Entertainment)] = [.. Enum.GetNames<Entertainment>()],
        [nameof(Health)] = [.. Enum.GetNames<Health>()],
        [nameof(Personal)] = [.. Enum.GetNames<Personal>()],
        [nameof(Other)] = [.. Enum.GetNames<Other>()],
        [nameof(Vacations)] = [.. Enum.GetNames<Vacations>()],
        [nameof(Charity)] = [.. Enum.GetNames<Charity>()]
    };

    public static string GetSubCategories(string mainCategory)
    {
        if (Categories.TryGetValue(mainCategory, out List<string>? subCategories))
        {
            return string.Join(", ", subCategories);
        }

        throw new ArgumentException($"Main category '{mainCategory}' does not exist.");
    }

    public static readonly ChatTool GetSubCategoriesTool = ChatTool.CreateFunctionTool(
        functionName: nameof(GetSubCategories),
        functionDescription: "Get subcategories for a given main category",
        functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "mainCategory": {
                    "type": "string",
                    "description": "The main category for which to retrieve subcategories."
                }
            },
            "required": [ "mainCategory" ]
        }
        """u8.ToArray())
    );
}