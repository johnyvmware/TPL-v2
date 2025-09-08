using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessingSystem.Models;

public record TransferIncoming(
    DateTime Date,
    decimal Amount,
    string Sender,
    string Title) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "PRZELEW ZEWNĘTRZNY PRZYCHODZĄCY",
        "PRZELEW WEWNĘTRZNY PRZYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    protected override string DisplayName => "Incoming Transfer";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out TransferIncoming? transferIncoming)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            transferIncoming = new TransferIncoming(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Sender: rawTransaction.ReceiverOrSender,
                Title: rawTransaction.Title);

            return true;
        }

        transferIncoming = null;
        return false;
    }

    protected override string DescribeProperties()
    {
        string description = $"""
            Title: {Title}
            Sender: {Sender}
            """;

        return description;
    }
}
