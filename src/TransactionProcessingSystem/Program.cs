using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configure application settings and secrets
builder.Services.AddApplicationConfiguration(builder.Configuration);

// Configure application services
builder.Services.AddApplicationServices();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var notUsed = scope.ServiceProvider.GetRequiredService<IOptions<LlmSettings>>(); // This is the limitation of the current design, we need to ensure LlmSettings are loaded before using them

var fetcher = scope.ServiceProvider.GetRequiredService<Fetcher>();
var categorizer = scope.ServiceProvider.GetRequiredService<Categorizer>();

List<RawTransaction> rawTransactions = fetcher.FetchTransactions();

List<Transaction> categorizedTransactions = [];
foreach (var transaction in rawTransactions)
{
    Transaction? categorizedTransaction = await categorizer.CategorizeTransactionAsync(transaction);
    if (categorizedTransaction != null)
    {
        categorizedTransactions.Add(categorizedTransaction);
    }
}

Console.ReadLine();