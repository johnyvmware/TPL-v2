using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;

namespace TransactionProcessingSystem.Components;

/// <summary>
/// Demo component showing how to properly inject and use validated configuration options.
/// This demonstrates the options pattern best practices.
/// </summary>
public sealed class ConfigurationInjectionDemo
{
    private readonly ILogger<ConfigurationInjectionDemo> _logger;
    private readonly AppSettings _appSettings;
    private readonly OpenAISettings _openAISettings;
    private readonly TransactionApiSettings _transactionApiSettings;
    private readonly PipelineSettings _pipelineSettings;
    private readonly Neo4jSettings _neo4jSettings;
    private readonly SecretsSettings _secretsSettings;

    public ConfigurationInjectionDemo(
        ILogger<ConfigurationInjectionDemo> logger,
        IOptions<AppSettings> appSettings,
        IOptions<OpenAISettings> openAISettings,
        IOptions<TransactionApiSettings> transactionApiSettings,
        IOptions<PipelineSettings> pipelineSettings,
        IOptions<Neo4jSettings> neo4jSettings,
        IOptions<SecretsSettings> secretsSettings)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _openAISettings = openAISettings.Value;
        _transactionApiSettings = transactionApiSettings.Value;
        _pipelineSettings = pipelineSettings.Value;
        _neo4jSettings = neo4jSettings.Value;
        _secretsSettings = secretsSettings.Value;
    }

    /// <summary>
    /// Demonstrates how to use the validated configuration options.
    /// Configuration validation has already occurred at startup, so these values are guaranteed to be valid.
    /// </summary>
    public async Task DemonstrateConfigurationUsageAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuration Injection Demo - Using validated configuration options");

        // Demonstrate OpenAI settings usage
        _logger.LogInformation("OpenAI Configuration:");
        _logger.LogInformation("  Model: {Model}", _openAISettings.Model);
        _logger.LogInformation("  Max Tokens: {MaxTokens}", _openAISettings.MaxTokens);
        _logger.LogInformation("  Temperature: {Temperature}", _openAISettings.Temperature);

        // Demonstrate Transaction API settings usage
        _logger.LogInformation("Transaction API Configuration:");
        _logger.LogInformation("  Base URL: {BaseUrl}", _transactionApiSettings.BaseUrl);
        _logger.LogInformation("  Timeout: {TimeoutSeconds}s", _transactionApiSettings.TimeoutSeconds);
        _logger.LogInformation("  Max Retries: {MaxRetries}", _transactionApiSettings.MaxRetries);
        _logger.LogInformation("  Batch Size: {BatchSize}", _transactionApiSettings.BatchSize);
        _logger.LogInformation("  Mock Data Enabled: {EnableMockData}", _transactionApiSettings.EnableMockData);

        // Demonstrate Pipeline settings usage
        _logger.LogInformation("Pipeline Configuration:");
        _logger.LogInformation("  Bounded Capacity: {BoundedCapacity}", _pipelineSettings.BoundedCapacity);
        _logger.LogInformation("  Max Degree of Parallelism: {MaxDegreeOfParallelism}", _pipelineSettings.MaxDegreeOfParallelism);
        _logger.LogInformation("  Timeout: {TimeoutMinutes} minutes", _pipelineSettings.TimeoutMinutes);

        // Demonstrate Neo4j settings usage
        _logger.LogInformation("Neo4j Configuration:");
        _logger.LogInformation("  Database: {Database}", _neo4jSettings.Database);
        _logger.LogInformation("  Max Connection Pool Size: {MaxConnectionPoolSize}", _neo4jSettings.MaxConnectionPoolSize);
        _logger.LogInformation("  Connection Timeout: {ConnectionTimeoutSeconds}s", _neo4jSettings.ConnectionTimeoutSeconds);
        _logger.LogInformation("  Max Transaction Retry Time: {MaxTransactionRetryTimeSeconds}s", _neo4jSettings.MaxTransactionRetryTimeSeconds);

        // Demonstrate secrets usage (without exposing sensitive data)
        _logger.LogInformation("Secrets Configuration:");
        _logger.LogInformation("  OpenAI API Key: {ApiKeyPrefix}...", 
            _secretsSettings.OpenAI.ApiKey[..Math.Min(10, _secretsSettings.OpenAI.ApiKey.Length)]);
        _logger.LogInformation("  Microsoft Graph Client ID: {ClientId}", _secretsSettings.MicrosoftGraph.ClientId);
        _logger.LogInformation("  Neo4j Connection URI: {ConnectionUri}", _secretsSettings.Neo4j.ConnectionUri);

        // Demonstrate business logic using validated configuration
        await DemonstrateBusinessLogicAsync(cancellationToken);
    }

    /// <summary>
    /// Demonstrates business logic that relies on validated configuration.
    /// Since validation occurred at startup, we can safely use these values.
    /// </summary>
    private async Task DemonstrateBusinessLogicAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Demonstrating business logic with validated configuration...");

        // Example: Calculate optimal batch processing based on configuration
        var optimalBatchSize = Math.Min(_transactionApiSettings.BatchSize, _pipelineSettings.BoundedCapacity);
        _logger.LogInformation("Optimal batch size: {OptimalBatchSize}", optimalBatchSize);

        // Example: Calculate timeout for operations
        var operationTimeout = TimeSpan.FromSeconds(Math.Min(_transactionApiSettings.TimeoutSeconds, 60));
        _logger.LogInformation("Operation timeout: {OperationTimeout}", operationTimeout);

        // Example: Validate OpenAI configuration for specific use case
        if (_openAISettings.MaxTokens > 1000)
        {
            _logger.LogInformation("Using high-capacity model for complex operations");
        }
        else
        {
            _logger.LogInformation("Using standard model for routine operations");
        }

        // Example: Check if mock data should be used
        if (_transactionApiSettings.EnableMockData)
        {
            _logger.LogInformation("Using mock data for development/testing");
        }
        else
        {
            _logger.LogInformation("Using real API data for production");
        }

        await Task.Delay(100, cancellationToken); // Simulate some work
        _logger.LogInformation("Business logic demonstration completed");
    }

    /// <summary>
    /// Demonstrates how to access nested configuration from the main AppSettings.
    /// This shows the relationship between individual settings and the main configuration.
    /// </summary>
    public void DemonstrateNestedConfigurationAccess()
    {
        _logger.LogInformation("Demonstrating nested configuration access...");

        // Access the same settings through the main AppSettings object
        var openAISettingsFromMain = _appSettings.OpenAI;
        var transactionApiSettingsFromMain = _appSettings.TransactionApi;
        var pipelineSettingsFromMain = _appSettings.Pipeline;
        var neo4jSettingsFromMain = _appSettings.Neo4j;

        // Verify they are the same instances (reference equality)
        var openAIMatch = ReferenceEquals(_openAISettings, openAISettingsFromMain);
        var transactionApiMatch = ReferenceEquals(_transactionApiSettings, transactionApiSettingsFromMain);
        var pipelineMatch = ReferenceEquals(_pipelineSettings, pipelineSettingsFromMain);
        var neo4jMatch = ReferenceEquals(_neo4jSettings, neo4jSettingsFromMain);

        _logger.LogInformation("Configuration reference equality check:");
        _logger.LogInformation("  OpenAI Settings Match: {OpenAIMatch}", openAIMatch);
        _logger.LogInformation("  Transaction API Settings Match: {TransactionApiMatch}", transactionApiMatch);
        _logger.LogInformation("  Pipeline Settings Match: {PipelineMatch}", pipelineMatch);
        _logger.LogInformation("  Neo4j Settings Match: {Neo4jMatch}", neo4jMatch);
    }
}

/// <summary>
/// Service registration extension to demonstrate different injection patterns
/// </summary>
public static class ConfigurationInjectionExtensions
{
    /// <summary>
    /// Registers the demo component with traditional IOptions pattern
    /// </summary>
    public static IServiceCollection AddConfigurationInjectionDemo(this IServiceCollection services)
    {
        // Traditional pattern - injects IOptions<T>
        services.AddScoped<ConfigurationInjectionDemo>();
        
        return services;
    }

    /// <summary>
    /// Registers the demo component with direct injection pattern (requires .NET 9)
    /// </summary>
    public static IServiceCollection AddConfigurationInjectionDemoWithDirectInjection(this IServiceCollection services)
    {
        // Direct injection pattern - injects configuration objects directly
        // This requires the constructor to accept the configuration objects directly
        // services.AddScoped<ConfigurationInjectionDemo>(serviceProvider =>
        // {
        //     var transactionApiSettings = serviceProvider.GetRequiredService<IOptions<TransactionApiSettings>>().Value;
        //     var openAISettings = serviceProvider.GetRequiredService<IOptions<OpenAISettings>>().Value;
        //     var logger = serviceProvider.GetRequiredService<ILogger<ConfigurationInjectionDemo>>();
        //     
        //     return new ConfigurationInjectionDemo(transactionApiSettings, openAISettings, logger);
        // });
        
        return services;
    }
} 