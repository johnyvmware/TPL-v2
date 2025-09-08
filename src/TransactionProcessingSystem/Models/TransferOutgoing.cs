using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessingSystem.Models;

public record TransferOutgoing(
    DateTime Date,
    decimal Amount,
    string Recipient,
    string Title) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "PRZELEW ZEWNĘTRZNY WYCHODZĄCY",
        "PRZELEW WEWNĘTRZNY WYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    protected override string DisplayName => "Outgoing Transfer";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out TransferOutgoing? transferOutgoing)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            transferOutgoing = new TransferOutgoing(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Recipient: rawTransaction.ReceiverOrSender,
                Title: rawTransaction.Title);

            return true;
        }

        transferOutgoing = null;
        return false;
    }

    protected override string DescribeProperties()
    {
        string description = $"""
            Title: {Title}
            Recipient: {Recipient}
            """;

        return description;
    }
}
