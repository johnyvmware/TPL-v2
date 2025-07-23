using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Pipeline;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Extension methods for configuring services following SRP and modern C# practices
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures all application settings and secrets with validation.
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureAppSettings(services, configuration);
        ConfigureAppSecrets(services, configuration);

        return services;
    }

    /// <summary>
    /// Adds Neo4j services including driver, data access, and background service.
    /// Follows SRP by keeping all Neo4j concerns together.
    /// </summary>
    public static IServiceCollection AddNeo4jServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Neo4j Driver as singleton
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4jSettings = serviceProvider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            var neo4jSecrets = serviceProvider.GetRequiredService<IOptions<Neo4jSecrets>>().Value;

            var authToken = AuthTokens.Basic(neo4jSecrets.Username, neo4jSecrets.Password);

            var driver = GraphDatabase.Driver(neo4jSecrets.ConnectionUri, authToken, config =>
            {
                config.WithMaxConnectionPoolSize(neo4jSettings.MaxConnectionPoolSize)
                      .WithConnectionTimeout(TimeSpan.FromSeconds(neo4jSettings.ConnectionTimeoutSeconds))
                      .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(neo4jSettings.MaxTransactionRetryTimeSeconds));
            });

            return driver;
        });

        // Register Neo4j data access services
        services.AddScoped<INeo4jDataAccess, Neo4jDataAccess>();

        // Note: Neo4jBackgroundService is commented out for demo purposes
        // services.AddHostedService<Neo4jBackgroundService>();

        return services;
    }

    /// <summary>
    /// Adds all processors including Neo4j exporter.
    /// </summary>
    public static IServiceCollection AddProcessors(this IServiceCollection services)
    {
        // Register all processors - Neo4jExporter is now registered in AddTransactionProcessingServices

        // Other processors can be added here
        // Example: services.AddScoped<ValidationProcessor>();

        return services;
    }

    /// <summary>
    /// Adds transaction processing services including pipeline and all components.
    /// </summary>
    public static IServiceCollection AddTransactionProcessingServices(this IServiceCollection services)
    {
        // Add HttpClient for TransactionFetcher
        services.AddHttpClient<TransactionFetcher>();

        // Register all pipeline components
        services.AddScoped<TransactionFetcher>();
        services.AddScoped<TransactionParser>();
        services.AddScoped<TransactionProcessor>();
        services.AddScoped<EmailEnricher>();
        services.AddScoped<Categorizer>();
        services.AddScoped<Neo4jExporter>();

        // Transaction processing pipeline
        services.AddScoped<TransactionPipeline>();

        return services;
    }

    private static void ConfigureAppSecrets(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptionsWithValidateOnStart<SecretsSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<OpenAISecrets>()
            .Bind(configuration.GetSection("Secrets:OpenAI"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphSecrets>()
            .Bind(configuration.GetSection("Secrets:MicrosoftGraph"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jSecrets>()
            .Bind(configuration.GetSection("Secrets:Neo4j"))
            .ValidateDataAnnotations();
    }

    private static void ConfigureAppSettings(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptionsWithValidateOnStart<AppSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<OpenAISettings>()
            .Bind(configuration.GetSection("OpenAI"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphSettings>()
            .Bind(configuration.GetSection("MicrosoftGraph"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<TransactionApiSettings>()
            .Bind(configuration.GetSection("TransactionApi"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<ExportSettings>()
            .Bind(configuration.GetSection("Export"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<PipelineSettings>()
            .Bind(configuration.GetSection("Pipeline"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jSettings>()
            .Bind(configuration.GetSection("Neo4j"))
            .ValidateDataAnnotations();

        services.AddSingleton<IValidateOptions<PipelineSettings>, PipelineSettingsValidator>();
    }
}