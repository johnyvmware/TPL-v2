using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Components;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);

// Register code pages for Windows-1250 encoding support
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Configure application settings and secrets with validation
builder.Services.AddApplicationConfiguration(builder.Configuration);

// Configure Neo4j services
builder.Services.AddNeo4jServices(builder.Configuration);

// Add transaction processing services
builder.Services.AddTransactionProcessingServices();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var transactionFetcher = scope.ServiceProvider.GetRequiredService<TransactionFetcher>();
await transactionFetcher.FetchTransactionsAsync();

// Optionally, run the host if you still want to keep the background services running
// await host.RunAsync();