using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services.Categorizer;

public interface ICategoryService
{
    bool IsValidCombination(string mainCategory, string subCategory);
    CategoryAssignmentResult ValidateCategorization(CategoryAssignment categorization);
    IEnumerable<string> GetSubCategories(string mainCategory);
    IEnumerable<string> GetMainCategories();
    bool IsValidMainCategory(string mainCategory);
    bool IsValidSubCategory(string subCategory);
}

public class CategoryService(ICategoryProvider categoriesProvider) : ICategoryService
{
    public IEnumerable<string> GetSubCategories(string mainCategory)
    {
        return categoriesProvider.ValidCombinations.TryGetValue(mainCategory, out var subCategories)
            ? subCategories
            : Enumerable.Empty<string>();
    }

    public IEnumerable<string> GetMainCategories()
    {
        return categoriesProvider.ValidCombinations.Keys;
    }

    public bool IsValidCombination(string mainCategory, string subCategory)
    {
        return categoriesProvider.ValidCombinations.TryGetValue(mainCategory, out var validSubCategories) && validSubCategories.Contains(subCategory);
    }

    public bool IsValidMainCategory(string mainCategory)
    {
        return categoriesProvider.MainCategories.Contains(mainCategory);
    }

    public bool IsValidSubCategory(string subCategory)
    {
        return categoriesProvider.SubCategories.Contains(subCategory);
    }

    public CategoryAssignmentResult ValidateCategorization(CategoryAssignment categorization)
    {
        if (string.IsNullOrWhiteSpace(categorization.MainCategory))
        {
            return CategoryAssignmentResult.Failure("Main category cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(categorization.SubCategory))
        {
            return CategoryAssignmentResult.Failure("Sub category cannot be empty");
        }

        if (!IsValidMainCategory(categorization.MainCategory))
        {
            return CategoryAssignmentResult.Failure(
                $"Unknown main category: '{categorization.MainCategory}'. " +
                $"Valid main categories: {string.Join(", ", GetMainCategories())}");
        }

        if (!IsValidSubCategory(categorization.SubCategory))
        {
            return CategoryAssignmentResult.Failure(
                $"Unknown sub category: '{categorization.SubCategory}'. " +
                $"All valid sub categories: {string.Join(", ", categoriesProvider.SubCategories)}");
        }

        if (!IsValidCombination(categorization.MainCategory, categorization.SubCategory))
        {
            var validSubCategories = GetSubCategories(categorization.MainCategory);
            return CategoryAssignmentResult.Failure(
                $"Invalid combination: '{categorization.MainCategory}' cannot have subcategory '{categorization.SubCategory}'. " +
                $"Valid subcategories for '{categorization.MainCategory}': {string.Join(", ", validSubCategories)}");
        }

        return CategoryAssignmentResult.Success();
    }
}