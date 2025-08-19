using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem;

internal sealed class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly Fetcher _fetcher;
    private readonly Categorizer _categorizer;
    private readonly Exporter _exporter;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        Fetcher fetcher,
        Categorizer categorizer,
        Exporter exporter)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _fetcher = fetcher;
        _categorizer = categorizer;
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
        await _exporter.VerifyConnectionAsync();
        //await _exporter.CreateGraphAsync();
        await _exporter.QueryGraphAsync();
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