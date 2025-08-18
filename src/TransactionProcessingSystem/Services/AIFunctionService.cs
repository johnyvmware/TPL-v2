using System.ComponentModel;
using Microsoft.Extensions.AI;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem.Services;

public class AIFunctionService(ICategoryService categoriesService)
{
    public AIFunction GetSubCategoriesAIFunction()
    {
        var get_sub_categories = AIFunctionFactory.Create(GetSubCategories, "get_sub_categories");

        return get_sub_categories;
    }

    public AIFunction GetMainCategoriesAIFunction()
    {
        var get_main_categories = AIFunctionFactory.Create(GetMainCategories, "get_main_categories");

        return get_main_categories;
    }

    [Description("Get valid sub categories for a given main category")]
    public IEnumerable<string> GetSubCategories(
        [Description("The main category name to get subcategories for")]
        string mainCategory)
    {
        return categoriesService.GetSubCategories(mainCategory);
    }

    [Description("Get all available main categories")]
    public IEnumerable<string> GetMainCategories()
    {
        return categoriesService.GetMainCategories();
    }
}
