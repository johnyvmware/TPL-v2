using System;

public record BankTransaction
{
    public DateTime PostingDate { get; init; }           // Data księgowania
    public DateTime OperationDate { get; init; }         // Data operacji
    public required string OperationDescription { get; init; }    // Opis operacji
    public required string Title { get; init; }                   // Tytuł
    public required string SenderOrRecipient { get; init; }       // Nadawca/Odbiorca
    public required string AccountNumber { get; init; }           // Numer konta
    public decimal Amount { get; init; }                 // Kwota
    public decimal BalanceAfterOperation { get; init; }  // Saldo po operacji
}
