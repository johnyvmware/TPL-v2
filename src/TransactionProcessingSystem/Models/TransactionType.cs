using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessingSystem.Models;

public record Transaction(DateTime Date, decimal Amount);

public record OtherTransaction(
    DateTime Date,
    decimal Amount,
    string Title,
    string Description,
    string Receiver
) : Transaction(Date, Amount)
{
    public static OtherTransaction Match(RawTransaction rawTransaction)
    {
        return new OtherTransaction(
            Date: rawTransaction.Date,
            Amount: rawTransaction.Amount,
            Title: rawTransaction.Title,
            Description: rawTransaction.Description,
            Receiver: rawTransaction.ReceiverOrSender
        );
    }
}

public record CardPurchase(
    DateTime Date,
    decimal Amount,
    string Store,
    string Location
) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "ZAKUP PRZY UŻYCIU KARTY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out CardPurchase? cardPurchase)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            cardPurchase = new CardPurchase(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Store: "parse", //rawTransaction.Store,
                Location: "parse" //rawTransaction.Location
            );

            return true;
        }

        cardPurchase = null;
        return false;
    }

}

public record BlikEcommercePurchase(
    DateTime Date,
    decimal Amount,
    string Store
) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "BLIK ZAKUP E-COMMERCE"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikEcommercePurchase? blikEcommercePurchase)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikEcommercePurchase = new BlikEcommercePurchase(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Store: "parse" //rawTransaction.Store
            );

            return true;
        }

        blikEcommercePurchase = null;
        return false;
    }
}

public record BlikP2PIncoming(
    DateTime Date,
    decimal Amount,
    string Title
) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "BLIK P2P-PRZYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikP2PIncoming? blikP2PIncoming)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikP2PIncoming = new BlikP2PIncoming(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Title: rawTransaction.Title
            );

            return true;
        }

        blikP2PIncoming = null;
        return false;
    }
};

public record BlikP2POutgoing(
    DateTime Date,
    decimal Amount,
    string Title
) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "BLIK P2P-WYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikP2POutgoing? blikP2POutgoing)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikP2POutgoing = new BlikP2POutgoing(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Title: rawTransaction.Title
            );

            return true;
        }

        blikP2POutgoing = null;
        return false;
    }
};

public record TransferOutgoing(
    DateTime Date,
    decimal Amount,
    string Recipient,
    string Title
) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "PRZELEW ZEWNĘTRZNY WYCHODZĄCY",
        "PRZELEW WEWNĘTRZNY WYCHODZĄCY"
    ];

    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out TransferOutgoing? transferOutgoing)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            transferOutgoing = new TransferOutgoing(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Recipient: rawTransaction.ReceiverOrSender,
                Title: rawTransaction.Title
            );

            return true;
        }

        transferOutgoing = null;
        return false;
    }
};

public record TransferIncoming(
    DateTime Date,
    decimal Amount,
    string Sender,
    string Title
) : Transaction(Date, Amount)
{
    private static readonly IReadOnlyCollection<string> SupportedTypes =
    [
        "PRZELEW ZEWNĘTRZNY PRZYCHODZĄCY",
        "PRZELEW WEWNĘTRZNY PRZYCHODZĄCY"
    ];
    private static bool IsSupportedType(string description) => SupportedTypes.Contains(description);

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out TransferIncoming? transferIncoming)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            transferIncoming = new TransferIncoming(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Sender: rawTransaction.ReceiverOrSender,
                Title: rawTransaction.Title
            );

            return true;
        }

        transferIncoming = null;
        return false;
    }
};