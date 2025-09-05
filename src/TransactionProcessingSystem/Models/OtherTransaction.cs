namespace TransactionProcessingSystem.Models;

public record OtherTransaction(
    DateTime Date,
    decimal Amount,
    string Title,
    string Description,
    string Receiver) : Transaction(Date, Amount)
{
    protected override string DisplayName => Description;

    public static OtherTransaction Match(RawTransaction rawTransaction)
    {
        return new OtherTransaction(
            Date: rawTransaction.Date,
            Amount: rawTransaction.Amount,
            Title: rawTransaction.Title,
            Description: rawTransaction.Description,
            Receiver: rawTransaction.ReceiverOrSender);
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
