using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Processors;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add Neo4j services with modern configuration
builder.Services.AddNeo4jServices(builder.Configuration);

// Neo4j Services
builder.Services.AddScoped<INeo4jDataAccess, Neo4jDataAccess>();
builder.Services.AddScoped<INeo4jReactiveDataAccess, Neo4jReactiveDataAccess>();

// Processors
builder.Services.AddScoped<Neo4jProcessor>();

// Pipeline
builder.Services.AddScoped<TransactionPipeline>();

// Background Service
builder.Services.AddHostedService<Neo4jBackgroundService>();

var host = builder.Build();

// Validate configuration at startup
try
{
    var neo4jSettings = host.Services.GetRequiredService<IOptions<Neo4jSettings>>().Value;
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Neo4j configuration validated successfully. Connected to: {ConnectionUri}", 
        neo4jSettings.ConnectionUri);
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to validate Neo4j configuration");
    throw;
}

// Run the application
await host.RunAsync();

/// <summary>
/// Background service for Neo4j operations
/// </summary>
public class Neo4jBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<Neo4jBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Neo4j Background Service starting...");

        try
        {
            using var scope = serviceProvider.CreateScope();
            var neo4jDataAccess = scope.ServiceProvider.GetRequiredService<INeo4jDataAccess>();
            var neo4jProcessor = scope.ServiceProvider.GetRequiredService<Neo4jProcessor>();

            // Initialize the processor
            await neo4jProcessor.InitializeAsync(stoppingToken);

            // Verify connectivity
            var isConnected = await neo4jDataAccess.VerifyConnectivityAsync(stoppingToken);
            if (!isConnected)
            {
                logger.LogError("Failed to connect to Neo4j database");
                return;
            }

            logger.LogInformation("Neo4j connection verified successfully");

            // Initialize database schema
            await neo4jDataAccess.InitializeDatabaseAsync(stoppingToken);
            logger.LogInformation("Neo4j database schema initialized");

            // Demo: Create sample transaction
            var sampleTransaction = new TransactionProcessingSystem.Models.Transaction
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                Amount = 125.50m,
                Description = "Sample transaction for Neo4j testing",
                Category = "Testing"
            };

            var transactionId = await neo4jDataAccess.UpsertTransactionAsync(sampleTransaction, stoppingToken);
            logger.LogInformation("Created sample transaction: {TransactionId}", transactionId);

            // Demo: Get analytics
            var analytics = await neo4jDataAccess.GetTransactionAnalyticsAsync(stoppingToken);
            logger.LogInformation("Analytics: {TotalTransactions} total transactions, {TotalAmount:C} total amount",
                analytics.TotalTransactions, analytics.TotalAmount);

            // Demo: Stream graph statistics
            await foreach (var statistic in neo4jDataAccess.GetGraphStatisticsAsync(stoppingToken))
            {
                logger.LogInformation("Graph Statistic: {Type} {Name} = {Count}",
                    statistic.Type, statistic.Name, statistic.Count);
            }

            logger.LogInformation("Neo4j operations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Neo4j Background Service");
        }
    }
}