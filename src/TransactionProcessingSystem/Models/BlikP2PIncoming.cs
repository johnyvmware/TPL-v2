using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessingSystem.Models;

public record BlikP2PIncoming(
    DateTime Date,
    decimal Amount,
    string Title) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "BLIK P2P-PRZYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    protected override string DisplayName => "Blik P2P Incoming";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikP2PIncoming? blikP2PIncoming)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikP2PIncoming = new BlikP2PIncoming(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Title: rawTransaction.Title);

            return true;
        }

        blikP2PIncoming = null;
        return false;
    }

    protected override string DescribeProperties()
    {
        return $"Title: {Title}";
    }
}
