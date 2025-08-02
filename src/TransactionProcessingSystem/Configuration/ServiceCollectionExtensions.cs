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
    /// </summary>
    public static IServiceCollection AddNeo4jServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Neo4j Driver as singleton
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4jSettings = serviceProvider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            var neo4jSecrets = serviceProvider.GetRequiredService<IOptions<Neo4jSecrets>>().Value;

            var authToken = AuthTokens.Basic(neo4jSecrets.User, neo4jSecrets.Password);

            var driver = GraphDatabase.Driver(neo4jSecrets.Uri, authToken, config =>
            {
                config.WithMaxConnectionPoolSize(neo4jSettings.MaxConnectionPoolSize)
                      .WithConnectionTimeout(TimeSpan.FromSeconds(neo4jSettings.ConnectionTimeoutSeconds))
                      .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(neo4jSettings.MaxTransactionRetryTimeSeconds));
            });

            return driver;
        });

        // Register Neo4j data access services
        services.AddScoped<INeo4jDataAccess, Neo4jDataAccess>();

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
            .AddOptions<SecretsSettings>()
            .Bind(configuration);

        services
            .AddOptionsWithValidateOnStart<OpenAISecrets>()
            .Bind(configuration.GetSection("OpenAI"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphSecrets>()
            .Bind(configuration.GetSection("MicrosoftGraph"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jSecrets>()
            .Bind(configuration.GetSection("Neo4j"))
            .ValidateDataAnnotations();
    }

    private static void ConfigureAppSettings(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AppSettings>()
            .Bind(configuration);

        services
            .AddOptionsWithValidateOnStart<OpenAISettings>()
            .Bind(configuration.GetSection("OpenAI"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphSettings>()
            .Bind(configuration.GetSection("MicrosoftGraph"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<ExportSettings>()
            .Bind(configuration.GetSection("Export"))
            .ValidateDataAnnotations();

        services
            .AddSingleton<IValidateOptions<PipelineSettings>, MaxDegreeOfParallelismValidator>()
            .AddOptionsWithValidateOnStart<PipelineSettings>()
            .Bind(configuration.GetSection("Pipeline"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jSettings>()
            .Bind(configuration.GetSection("Neo4j"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<TransactionFetcherSettings>()
            .Bind(configuration.GetSection("TransactionFetcher"))
            .ValidateDataAnnotations();
    }
}