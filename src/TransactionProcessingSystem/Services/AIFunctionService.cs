using System.ComponentModel;
using Microsoft.Extensions.AI;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem.Services;

public class AIFunctionService(CategoryProviderV2 categoryProvider)
{
    public AIFunction GetSubCategoriesAIFunction()
    {
        var getSubCategories = AIFunctionFactory.Create(GetSubCategories, "get_sub_categories");

        return getSubCategories;
    }

    public AIFunction GetMainCategoriesAIFunction()
    {
        var getMainCategories = AIFunctionFactory.Create(GetMainCategories, "get_main_categories");

        return getMainCategories;
    }

    [Description("Get sub categories and their definition for a given main category")]
    public IEnumerable<CategoryInfo> GetSubCategories(
        [Description("The main category name to get subcategories for")]
        string mainCategory)
    {
        return categoryProvider.GetSubCategoriesFor(mainCategory);
    }

    [Description("Get main categories and their definition")]
    public IEnumerable<CategoryInfo> GetMainCategories()
    {
        return categoryProvider.GetMainCategories();
    }
}
