using System.Text.Json.Serialization;

namespace TransactionProcessingSystem.Models.Categories;

// charity and vacation missing
public enum Subcategory
{
    None,
    Groceries,
    Books,
    Courses,
    Recreation,
    Credit,
    Food,
    Doctor,
    Medicines,
    Media,
    Utilities,
    Furniture,
    Consumables,
    Bonds,
    Tools,
    Clothes,
    Beauty,
    Fuel,
    Repairs,
    PublicTransport,
    Other
}