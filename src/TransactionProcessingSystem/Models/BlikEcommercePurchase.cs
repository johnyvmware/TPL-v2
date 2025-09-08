using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessingSystem.Models;

public record BlikEcommercePurchase(
    DateTime Date,
    decimal Amount,
    string Store) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> s_supportedTypes =
    [
        "BLIK ZAKUP E-COMMERCE"
    ];

    private static bool IsSupportedType(string description) => s_supportedTypes.Contains(description);

    protected override string DisplayName => "Blik E-Commerce Purchase";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikEcommercePurchase? blikEcommercePurchase)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikEcommercePurchase = new BlikEcommercePurchase(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Store: rawTransaction.Title);

            return true;
        }

        blikEcommercePurchase = null;
        return false;
    }

    protected override string DescribeProperties()
    {
        return $"Store: {Store}";
    }
}
