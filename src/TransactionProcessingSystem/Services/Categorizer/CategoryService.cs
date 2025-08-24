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

public class CategoryService(CategoryProviderV2 categoriesProvider) : ICategoryService
{
    public IEnumerable<Subcategory> GetSubCategories(string mainCategory)
    {
        return categoriesProvider.GetSubcategories(mainCategory);
    }

    public IEnumerable<string> GetMainCategories()
    {
        return categoriesProvider.Categories.Select(c => c.Name);
    }

    public bool IsValidCombination(string mainCategory, string subCategory)
    {
        return categoriesProvider.Categories.FirstOrDefault(c => c.Name == mainCategory)?.Subcategories.Select(sc => sc.Name)
            .Contains(subCategory) ?? false;
    }

    public bool IsValidMainCategory(string mainCategory)
    {
        return categoriesProvider.Categories.Any(c => c.Name == mainCategory);
    }

    public bool IsValidSubCategory(string subCategory)
    {
        return categoriesProvider.Categories.SelectMany(c => c.Subcategories).Any(sc => sc.Name == subCategory);
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
                $"All valid sub categories: {string.Join(", ", categoriesProvider.Categories.SelectMany(c => c.Subcategories).Select(sc => sc.Name))}");
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