using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;

var builder = Host.CreateApplicationBuilder(args);

// Configure application settings and secrets
builder.Services.AddApplicationConfiguration(builder.Configuration);

// Configure application services
builder.Services.AddApplicationServices();

var host = builder.Build();

using var scope = host.Services.CreateScope();

var fetcher = scope.ServiceProvider.GetRequiredService<Fetcher>();
var titleFormatter = scope.ServiceProvider.GetRequiredService<TitleFormatter>();

List<RawTransaction> transactions = fetcher.FetchTransactions();
List<Transaction> transactionWithFormatterTitle = [];
foreach (var transaction in transactions)
{
    Transaction? formattedTransaction = await titleFormatter.CategorizeTransactionAsync(transaction).ConfigureAwait(false);
    if (formattedTransaction != null)
    {
        transactionWithFormatterTitle.Add(formattedTransaction);
    }
}

Console.ReadLine();