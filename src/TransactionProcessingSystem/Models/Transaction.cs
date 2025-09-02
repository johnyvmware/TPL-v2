using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TransactionProcessingSystem.Models;

public abstract record Transaction(DateTime Date, decimal Amount)
{
    protected abstract string DisplayName { get; }

    public CategoryAssignment? CategoryAssignment { get; init; }

    protected abstract string DescribeProperties();

    public string Describe()
    {
        string description = $"""
            Transaction type: {DisplayName}
            {DescribeProperties()}
            """;

        return description;
    }
}

public record OtherTransaction(
    DateTime Date,
    decimal Amount,
    string Title,
    string Description,
    string Receiver
) : Transaction(Date, Amount)
{
    protected override string DisplayName => Description;

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

    protected override string DescribeProperties()
    {
        string description = $"""
            Title: {Title}
            """;
        
        if (!string.IsNullOrEmpty(Receiver))
        {
            description += $"""
            Receiver: {Receiver}
            """;
        }
        
        return description;
    }
}

public record CardPurchase(
    DateTime Date,
    decimal Amount,
    string Store,
    string Location,
    string Title
) : Transaction(Date, Amount)
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
                Title: rawTransaction.Title
            );

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
            sb.Append($"Store: {Store}\n");
        }

        if (!string.IsNullOrEmpty(Location))
        {
            sb.Append($"Location: {Location}\n");
        }

        // Only include Title if either Store or Location is missing
        if (string.IsNullOrEmpty(Store) || string.IsNullOrEmpty(Location))
        {
            sb.Append($"Title: {Title}\n");
        }

        return sb.ToString().TrimEnd('\n');
    }

    private static (string StoreName, string Location) ParseDescription(string description)
    {
        int dateIndex = description.IndexOf("DATA TRANSAKCJI:");
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

    protected override string DisplayName => "Blik E-Commerce Purchase";

    public static bool TryMatch(RawTransaction rawTransaction, [NotNullWhen(true)] out BlikEcommercePurchase? blikEcommercePurchase)
    {
        if (IsSupportedType(rawTransaction.Description))
        {
            blikEcommercePurchase = new BlikEcommercePurchase(
                Date: rawTransaction.Date,
                Amount: rawTransaction.Amount,
                Store: rawTransaction.Title
            );

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

    protected override string DisplayName => "Blik P2P Incoming";

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

    protected override string DescribeProperties()
    {
        return $"Title: {Title}";
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

    protected override string DisplayName => "Blik P2P Outgoing";

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

    protected override string DescribeProperties()
    {
        return $"Title: {Title}";
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

    protected override string DisplayName => "Outgoing Transfer";

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

    protected override string DescribeProperties()
    {
        string description = $"""
            Title: {Title}
            Recipient: {Recipient}
            """;

        return description;
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

    protected override string DisplayName => "Incoming Transfer";

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

    protected override string DescribeProperties()
    {
        string description = $"""
            Title: {Title}
            Sender: {Sender}
            """;

        return description;
    }
};