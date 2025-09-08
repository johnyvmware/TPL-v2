using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TransactionProcessingSystem.Models;

public record CardPurchase(
    DateTime Date,
    decimal Amount,
    string Store,
    string Location,
    string Title) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "ZAKUP PRZY UŻYCIU KARTY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    protected override string DisplayName => "Card Purchase";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out CardPurchase? cardPurchase)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            (string storeName, string location) = ParseDescription(rawTransaction.Title);

            cardPurchase = new CardPurchase(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Store: storeName,
                Location: location,
                Title: rawTransaction.Title);

            return true;
        }

        cardPurchase = null;
        return false;
    }

    protected override string DescribeProperties()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(Store))
        {
            sb.Append(FormattableString.Invariant($"Store: {Store}\n"));
        }

        if (!string.IsNullOrEmpty(Location))
        {
            sb.Append(FormattableString.Invariant($"Location: {Location}\n"));
        }

        // Only include Title if either Store or Location is missing
        if (string.IsNullOrEmpty(Store) || string.IsNullOrEmpty(Location))
        {
            sb.Append(FormattableString.Invariant($"Title: {Title}\n"));
        }

        return sb.ToString().TrimEnd('\n');
    }

    private static (string StoreName, string Location) ParseDescription(string description)
    {
        int dateIndex = description.IndexOf("DATA TRANSAKCJI:", StringComparison.InvariantCultureIgnoreCase);
        if (dateIndex > 0)
        {
            description = description[..dateIndex].Trim();
        }

        int slashIndex = description.LastIndexOf('/');
        if (slashIndex < 0)
        {
            return (description.Trim(), string.Empty);
        }

        string storeName = description[..slashIndex].Trim();
        string location = description[(slashIndex + 1)..].Trim();

        return (storeName, location);
    }
}
