using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem;

internal sealed class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly Fetcher _fetcher;
    //private readonly Categorizer _categorizer;
    private readonly Categorizer _categorizerV2;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        Fetcher fetcher,
        Categorizer categorizerV2)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _fetcher = fetcher;
        //_categorizer = categorizer;
        _categorizerV2 = categorizerV2;

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
         List<RawTransaction> rawTransactions = _fetcher.Fetch();
        await _categorizerV2.CategorizeAsync(rawTransactions[10]);

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