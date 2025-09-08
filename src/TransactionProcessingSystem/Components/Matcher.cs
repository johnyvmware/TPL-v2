using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class Matcher
{
    public static List<Transaction> Match(List<RawTransaction> rawTransactions)
    {
        var transactions = new List<Transaction>();

        foreach (var raw in rawTransactions)
        {
            Transaction transaction = Match(raw);
            transactions.Add(transaction);
        }

        return transactions;
    }

    private static Transaction Match(RawTransaction rawTransaction)
    {
        return rawTransaction switch
        {
            var t when BlikEcommercePurchase.TryMatch(t, out var blikEcommercePurchase) => blikEcommercePurchase,
            var t when BlikP2PIncoming.TryMatch(t, out var blikP2PIncoming) => blikP2PIncoming,
            var t when BlikP2POutgoing.TryMatch(t, out var blikP2POutgoing) => blikP2POutgoing,
            var t when CardPurchase.TryMatch(t, out var cardPurchase) => cardPurchase,
            var t when TransferOutgoing.TryMatch(t, out var transferOutgoing) => transferOutgoing,
            var t when TransferIncoming.TryMatch(t, out var transferIncoming) => transferIncoming,
            _ => OtherTransaction.Match(rawTransaction),
        };
    }
}
