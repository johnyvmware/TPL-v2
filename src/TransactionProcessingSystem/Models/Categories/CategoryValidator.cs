using TransactionProcessingSystem.Tools;

namespace TransactionProcessingSystem.Models.Categories;

public static class CategoryValidator
{
    public static bool IsValidMainCategory(string mainCategory)
    {
        return CategoryRegistry.Categories.ContainsKey(mainCategory);
    }

    public static bool IsValidSubCategory(string mainCategory, string subCategory)
    {
        if (CategoryRegistry.Categories.TryGetValue(mainCategory, out List<string>? subCategories))
        {
            return subCategories.Contains(subCategory);
        }
        return false;
    }

    public static bool IsValidCategorization(Categorization categorization)
    {
        return IsValidMainCategory(categorization.MainCategory) &&
               IsValidSubCategory(categorization.MainCategory, categorization.SubCategory);
    }
}