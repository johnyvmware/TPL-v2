using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TransactionProcessingSystem.Services.Categorizer;

public interface ICategoryProvider
{
    HashSet<string> MainCategories { get; }
    HashSet<string> SubCategories { get; }
    IReadOnlyDictionary<string, HashSet<string>> ValidCombinations { get; }
}

public class CategoryProvider(string categoriesFilePath, ILogger<CategoryProvider> logger) : ICategoryProvider
{
    private readonly Dictionary<string, HashSet<string>> _validCombinations = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mainCategories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _subCategories = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    public HashSet<string> MainCategories => _mainCategories;

    public HashSet<string> SubCategories => _subCategories;

    public IReadOnlyDictionary<string, HashSet<string>> ValidCombinations => _validCombinations;

    public void Load()
    {
        try
        {
            string jsonContent = File.ReadAllText(categoriesFilePath);
            InternalStorage? internalStorage = JsonSerializer.Deserialize<InternalStorage>(jsonContent, _jsonOptions);

            switch (internalStorage)
            {
                case null:
                    throw new InvalidDataException("Categories configuration is missing or null");
                case { Categories.Count: 0 }:
                    throw new InvalidDataException("Categories configuration is empty");
                case { Categories: var categories }:
                    ValidateAndBuildCombinations(categories);
                    break;
            }

        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON in categories file", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load categories", ex);
        }
    }

    private void ValidateAndBuildCombinations(Dictionary<string, List<string>> categories)
    {
        var errors = new List<string>();

        foreach (var (mainCategory, subCategories) in categories)
        {
            if (string.IsNullOrWhiteSpace(mainCategory))
            {
                errors.Add("Found empty or null main category");
                continue;
            }

            if (subCategories == null)
            {
                errors.Add($"Subcategories list is null for main category '{mainCategory}'");
                continue;
            }

            if (subCategories.Count == 0)
            {
                logger.LogWarning("Main category '{MainCategory}' has no subcategories", mainCategory);
            }

            var validSubCategories = subCategories
                .Where(sub => !string.IsNullOrWhiteSpace(sub))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (validSubCategories.Count != subCategories.Count)
            {
                var invalidCount = subCategories.Count - validSubCategories.Count;
                logger.LogWarning("Filtered out {InvalidCount} empty/null subcategories from '{MainCategory}'", invalidCount, mainCategory);
            }

            _validCombinations[mainCategory] = validSubCategories;
            _mainCategories.Add(mainCategory);
            foreach (var subCategory in validSubCategories)
            {
                _subCategories.Add(subCategory);
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException($"Category configuration validation failed:\n" + string.Join("\n", errors.Select(e => $"- {e}")));
        }

        if (_validCombinations.Count == 0)
        {
            throw new InvalidDataException($"No valid categories found in categories file");
        }

        logger.LogInformation("Successfully loaded {MainCategoryCount} main categories with {SubCategoryCount} total subcategories",
            _validCombinations.Count,
            _validCombinations.Values.Sum(x => x.Count));
    }

    internal record InternalStorage
    {
        public Dictionary<string, List<string>> Categories { get; set; } = [];
    }
}
