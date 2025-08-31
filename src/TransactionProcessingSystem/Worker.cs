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
    private readonly CategoryProviderV2 _categoryProvider;
    private readonly Exporter _exporter;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        Fetcher fetcher,
        Categorizer categorizer,
        CategoryProviderV2 categoryProvider,
        Exporter exporter)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _fetcher = fetcher;
        _categorizer = categorizer;
        _categoryProvider = categoryProvider;
        _exporter = exporter;

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
        _fetcher.Fetch();

        
        //await _exporter.CreateGraphAsync();
        List<Transaction> rawTransactions = _fetcher.Fetch();
        Transaction categorization = await _categorizer.CategorizeAsync(rawTransactions[10]);
        
        
        if (categorization != null)
        {
            // export it
        }

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