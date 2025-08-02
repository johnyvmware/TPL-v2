using System;
using CsvHelper.Configuration.Attributes;

[Delimiter(";")]
[CultureInfo("pl-PL")]
public record BankTransaction
{
    [Index(0)]
    public DateTime PostingDate { get; init; }

    [Index(1)]
    public DateTime OperationDate { get; init; }

    [Index(2)]
    public string? OperationDescription { get; init; }    // Opis operacji

    [Index(3)]
    public string? Title { get; init; }                   // Tytuł

    [Index(4)]
    public string? SenderOrRecipient { get; init; }       // Nadawca/Odbiorca

    [Index(5)]
    public string? AccountNumber { get; init; }           // Numer konta

    [Index(6)]
    public decimal Amount { get; init; }                 // Kwota

    [Index(7)]
    public decimal BalanceAfterOperation { get; init; }  // Saldo po operacji
}
