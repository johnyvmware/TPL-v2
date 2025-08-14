using System.ComponentModel;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

public interface ICategoriesService
{
    bool IsValidCombination(string mainCategory, string subCategory);
    ValidationResult ValidateCategorization(Categorization categorization); // maybe use data annotation?
    IEnumerable<string> GetSubCategories(string mainCategory);
    IEnumerable<string> GetMainCategories();
    bool IsValidMainCategory(string mainCategory);
    bool IsValidSubCategory(string subCategory);
}

public class CategoryService(ICategoriesProvider categoriesProvider) : ICategoriesService
{
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

    public ValidationResult ValidateCategorization(Categorization categorization)
    {
        if (string.IsNullOrWhiteSpace(categorization.MainCategory))
        {
            return ValidationResult.Failure("Main category cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(categorization.SubCategory))
        {
            return ValidationResult.Failure("Sub category cannot be empty");
        }

        if (!IsValidMainCategory(categorization.MainCategory))
        {
            return ValidationResult.Failure(
                $"Unknown main category: '{categorization.MainCategory}'. " +
                $"Valid main categories: {string.Join(", ", GetMainCategories())}");
        }

        if (!IsValidSubCategory(categorization.SubCategory))
        {
            return ValidationResult.Failure(
                $"Unknown sub category: '{categorization.SubCategory}'. " +
                $"All valid sub categories: {string.Join(", ", categoriesProvider.SubCategories)}");
        }

        if (!IsValidCombination(categorization.MainCategory, categorization.SubCategory))
        {
            var validSubCategories = GetSubCategories(categorization.MainCategory);
            return ValidationResult.Failure(
                $"Invalid combination: '{categorization.MainCategory}' cannot have subcategory '{categorization.SubCategory}'. " +
                $"Valid subcategories for '{categorization.MainCategory}': {string.Join(", ", validSubCategories)}");
        }

        return ValidationResult.Success();
    }

    [Description("Get valid sub categories for a given main category")]
    public IEnumerable<string> GetSubCategories(
        [Description("The main category name to get subcategories for")]
        string mainCategory)
    {
        return categoriesProvider.ValidCombinations.TryGetValue(mainCategory, out var subCategories)
            ? subCategories
            : Enumerable.Empty<string>();
    }

    [Description("Get all available main categories")]
    public IEnumerable<string> GetMainCategories()
    {
        return categoriesProvider.ValidCombinations.Keys;
    }
}