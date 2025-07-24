using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Components;

var builder = Host.CreateApplicationBuilder(args);

// Configure application settings and secrets with validation
builder.Services.AddApplicationConfiguration(builder.Configuration);

// Configure Neo4j services
builder.Services.AddNeo4jServices(builder.Configuration);

// Add transaction processing services
builder.Services.AddTransactionProcessingServices();

var host = builder.Build();

// Run the application
await host.RunAsync();