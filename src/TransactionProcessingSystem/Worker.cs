using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem;

internal sealed class Worker
{
    private readonly Fetcher _fetcher;
    private readonly Categorizer _categorizer;
    private readonly CategoryProvider _categoryProvider;
    private readonly Enricher _enricher;
    //private readonly Exporter _exporter;

    public Worker(
        Fetcher fetcher,
        Categorizer categorizer,
        CategoryProvider categoryProvider,
        Enricher enricher,
        Exporter _)
    {
        _fetcher = fetcher;
        _categorizer = categorizer;
        _categoryProvider = categoryProvider;
        _enricher = enricher;
        //_exporter = exporter;
    }

    public async Task ExecuteAsync()
    {
        await _enricher.EnrichAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        // Step 1: Load categories
        await _categoryProvider.LoadAsync();

        // Step 2: Fetch transactions
        List<RawTransaction> rawTransactions = _fetcher.Fetch();

        // Step 3: Match raw transaction to transaction type
        List<Transaction> transactions = Matcher.Match(rawTransactions);

        // Step 4: Categorize
        _ = await _categorizer.CategorizeAsync(transactions.First());

        // TODO: I need to have telemetry inspector = net aspire
        /*         List<RawTransaction> rawTransactions = _fetcher.FetchTransactions();
                                List<Transaction> categorizedTransactions = [];
                                foreach (var transaction in rawTransactions.Skip(2))
                                {
                                    Transaction? categorizedTransaction = await _categorizer.CategorizeTransactionAsync(transaction);
                                    if (categorizedTransaction != null)
                                    {
                                        categorizedTransactions.Add(categorizedTransaction);
                                    }
                                } */
    }
}
