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

var context = SynchronizationContext.Current;

var fetcher = scope.ServiceProvider.GetRequiredService<Fetcher>();
var titleFormatter = scope.ServiceProvider.GetRequiredService<Categorizer>();

List<RawTransaction> transactions = fetcher.FetchTransactions();

// I see its better to categorize directly without cleaning the title first
// Still I can store in the database all information, even the raw, this allows for better inspection and debugging
List<Transaction> transactionWithFormatterTitle = [];
foreach (var transaction in transactions)
{
    Transaction? formattedTransaction = await titleFormatter.CategorizeTransactionAsync(transaction);
    if (formattedTransaction != null)
    {
        transactionWithFormatterTitle.Add(formattedTransaction);
    }
}

Console.ReadLine();