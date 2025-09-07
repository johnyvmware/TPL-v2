using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem;

internal sealed class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly Fetcher _fetcher;
    private readonly Categorizer _categorizer;
    private readonly CategoryProvider _categoryProvider;
    //private readonly Exporter _exporter;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        Fetcher fetcher,
        Categorizer categorizer,
        CategoryProvider categoryProvider,
        Exporter _)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _fetcher = fetcher;
        _categorizer = categorizer;
        _categoryProvider = categoryProvider;
        //_exporter = exporter;

        _hostApplicationLifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("Transaction Processing System started.");
        });

        _hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            Console.WriteLine("Transaction Processing System stopped.");
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        _hostApplicationLifetime.StopApplication();
    }
}
