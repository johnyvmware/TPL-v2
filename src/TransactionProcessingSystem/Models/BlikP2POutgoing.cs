using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessingSystem.Models;

public record BlikP2POutgoing(
    DateTime Date,
    decimal Amount,
    string Title) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "BLIK P2P-WYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    protected override string DisplayName => "Blik P2P Outgoing";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikP2POutgoing? blikP2POutgoing)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikP2POutgoing = new BlikP2POutgoing(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Title: rawTransaction.Title);

            return true;
        }

        blikP2POutgoing = null;
        return false;
    }

    protected override string DescribeProperties()
    {
        return $"Title: {Title}";
    }
}
