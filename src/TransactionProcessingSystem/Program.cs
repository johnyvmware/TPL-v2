using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Processors;

var builder = Host.CreateApplicationBuilder(args);

// Host.CreateApplicationBuilder already handles:
// - User secrets for development environment
// - Environment variables for production
// - Basic configuration sources
// - Logging configuration from appsettings

// Configure application settings and secrets with validation
builder.Services.AddApplicationConfiguration(builder.Configuration);

// Configure Neo4j services
builder.Services.AddNeo4jServices(builder.Configuration);

// Add processors
builder.Services.AddProcessors();

// Add transaction processing services
builder.Services.AddTransactionProcessingServices();

var host = builder.Build();

// Validate all configuration at startup
ValidateConfiguration(host.Services);

// Run the application
await host.RunAsync();

/// <summary>
/// Validates all configuration and secrets at startup with proper error handling
/// </summary>
static void ValidateConfiguration(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Validating application configuration...");

        // Validate application settings (includes Neo4j settings validation)
        var appSettings = services.GetRequiredService<IOptions<AppSettings>>();
        var appValidator = services.GetRequiredService<IValidateOptions<AppSettings>>();

        var appValidationResult = appValidator.Validate(Options.DefaultName, appSettings.Value);
        if (appValidationResult.Failed)
        {
            var errors = string.Join(", ", appValidationResult.Failures);
            logger.LogError("Application settings validation failed: {Errors}", errors);
            throw new InvalidOperationException($"Application settings validation failed: {errors}");
        }

        logger.LogInformation("Application settings validated successfully. Transaction API: {BaseUrl}",
            appSettings.Value.TransactionApi.BaseUrl);

        // Validate secrets (includes Neo4j secrets validation)
        var secrets = services.GetRequiredService<IOptions<SecretsSettings>>();
        var secretsValidator = services.GetRequiredService<IValidateOptions<SecretsSettings>>();

        var secretsValidationResult = secretsValidator.Validate(Options.DefaultName, secrets.Value);
        if (secretsValidationResult.Failed)
        {
            var errors = string.Join(", ", secretsValidationResult.Failures);
            logger.LogError("Secrets validation failed: {Errors}", errors);
            throw new InvalidOperationException($"Secrets validation failed: {errors}");
        }

        logger.LogInformation("All configuration and secrets validated successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Configuration validation failed");
        throw;
    }
}

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
            var neo4jExporter = scope.ServiceProvider.GetRequiredService<Neo4jExporter>();

            // Initialize the exporter
            await neo4jExporter.InitializeAsync(stoppingToken);

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