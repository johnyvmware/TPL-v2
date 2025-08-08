using TransactionProcessingSystem.Models.Categories;

namespace TransactionProcessingSystem.Models;

public record ExpenseCategorization(
    string Reasoning,
    MainCategory MainCategory,
    Subcategory Subcategory
);