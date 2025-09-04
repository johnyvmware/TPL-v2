using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services.Categorizer;

public interface ICategoryValidator
{
    CategoryAssignmentResult Validate(CategoryAssignment categorization);
}

public class CategoryService(CategoryProvider categoriesProvider) : ICategoryValidator
{
    // simplify!
    public CategoryAssignmentResult Validate(CategoryAssignment categorization)
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
                $"Valid main categories: {string.Join(", ", categoriesProvider.GetMainCategories())}");
        }

        if (!IsValidSubCategory(categorization.SubCategory))
        {
            return CategoryAssignmentResult.Failure(
                $"Unknown sub category: '{categorization.SubCategory}'. " +
                $"All valid sub categories: {string.Join(", ", categoriesProvider.GetSubCategories())}");
        }

        if (!IsValidCombination(categorization.MainCategory, categorization.SubCategory))
        {
            var validSubCategories = categoriesProvider.GetSubCategoriesFor(categorization.MainCategory);
            return CategoryAssignmentResult.Failure(
                $"Invalid combination: '{categorization.MainCategory}' cannot have subcategory '{categorization.SubCategory}'. " +
                $"Valid subcategories for '{categorization.MainCategory}': {string.Join(", ", validSubCategories)}");
        }

        return CategoryAssignmentResult.Success();
    }

    private bool IsValidMainCategory(string mainCategory)
    {
        return categoriesProvider.GetMainCategories().Any(c => c.Name == mainCategory);
    }

    private bool IsValidSubCategory(string subCategory)
    {
        return categoriesProvider.GetSubCategories().Any(c => c.Name == subCategory);
    }

    private bool IsValidCombination(string mainCategory, string subCategory)
    {
        var validSubCategories = categoriesProvider.GetSubCategoriesFor(mainCategory);
        return validSubCategories.Any(sc => sc.Name == subCategory);
    }
}