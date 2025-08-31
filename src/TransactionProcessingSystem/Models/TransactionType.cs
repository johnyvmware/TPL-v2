namespace TransactionProcessingSystem.Models;

public abstract record TransactionType
{
    // Base transaction type
}

// ZAKUP PRZY UŻYCIU KARTY
public record CardPurchase(
    string Store,
    string Location
) : TransactionType;

// BLIK ZAKUP E-COMMERCE
public record BlikEcommercePurchase(
    string Store
) : TransactionType;

// BLIK P2P-PRZYCHODZĄCY
public record BlikP2PIncoming() : TransactionType;

// BLIK P2P-WYCHODZĄCY
public record BlikP2POutgoing() : TransactionType;

// PRZELEW WEWNĘTRZNY / ZEWNĘTRZNY WYCHODZĄCY
public record TransferOutgoing(
    string Recipient,
    string Title
) : TransactionType;

// PRZELEW WEWNĘTRZNY / ZEWNĘTRZNY PRZYCHODZĄCY
public record TransferIncoming(
    string Sender,
    string Title
) : TransactionType;