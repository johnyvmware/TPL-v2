using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Processors;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Extension methods for configuring services following SRP and modern C# practices
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures all application settings and secrets with comprehensive validation.
    /// Keeps settings and secrets separate as requested.
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure application settings (non-secrets)
        services.Configure<AppSettings>(configuration);
        services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
        services.Configure<MicrosoftGraphSettings>(configuration.GetSection("MicrosoftGraph"));
        services.Configure<TransactionApiSettings>(configuration.GetSection("TransactionApi"));
        services.Configure<ExportSettings>(configuration.GetSection("Export"));
        services.Configure<PipelineSettings>(configuration.GetSection("Pipeline"));
        services.Configure<Neo4jSettings>(configuration.GetSection("Neo4j"));

        // Configure secrets separately (from User Secrets in dev, Environment Variables in prod)
        services.Configure<SecretsSettings>(configuration);
        services.Configure<OpenAISecrets>(configuration.GetSection("Secrets:OpenAI"));
        services.Configure<MicrosoftGraphSecrets>(configuration.GetSection("Secrets:MicrosoftGraph"));
        services.Configure<Neo4jSecrets>(configuration.GetSection("Secrets:Neo4j"));

        // Register all individual validators
        services.AddSingleton<IValidateOptions<OpenAISettings>, OpenAISettingsValidator>();
        services.AddSingleton<IValidateOptions<MicrosoftGraphSettings>, MicrosoftGraphSettingsValidator>();
        services.AddSingleton<IValidateOptions<TransactionApiSettings>, TransactionApiSettingsValidator>();
        services.AddSingleton<IValidateOptions<ExportSettings>, ExportSettingsValidator>();
        services.AddSingleton<IValidateOptions<PipelineSettings>, PipelineSettingsValidator>();
        services.AddSingleton<IValidateOptions<Neo4jSettings>, Neo4jSettingsValidator>();

        services.AddSingleton<IValidateOptions<OpenAISecrets>, OpenAISecretsValidator>();
        services.AddSingleton<IValidateOptions<MicrosoftGraphSecrets>, MicrosoftGraphSecretsValidator>();
        services.AddSingleton<IValidateOptions<Neo4jSecrets>, Neo4jSecretsValidator>();

        // Register composite validators that handle all nested settings gracefully
        services.AddSingleton<IValidateOptions<AppSettings>, AppSettingsValidator>();
        services.AddSingleton<IValidateOptions<SecretsSettings>, SecretsSettingsValidator>();

        return services;
    }

    /// <summary>
    /// Single Neo4j bootstrap call that handles everything Neo4j related.
    /// Follows SRP by keeping all Neo4j concerns together.
    /// </summary>
    public static IServiceCollection AddNeo4jBootstrap(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Neo4j Driver as singleton with proper configuration
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4jSettings = serviceProvider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            var neo4jSecrets = serviceProvider.GetRequiredService<IOptions<Neo4jSecrets>>().Value;

            // Basic validation - detailed validation is handled by IValidateOptions
            if (string.IsNullOrWhiteSpace(neo4jSecrets.ConnectionUri) ||
                string.IsNullOrWhiteSpace(neo4jSecrets.Username) ||
                string.IsNullOrWhiteSpace(neo4jSecrets.Password))
            {
                throw new InvalidOperationException(
                    "Neo4j configuration is invalid. Please check ConnectionUri, Username, and Password in your secrets configuration.");
            }

            var authToken = AuthTokens.Basic(neo4jSecrets.Username, neo4jSecrets.Password);

            var driver = GraphDatabase.Driver(neo4jSecrets.ConnectionUri, authToken, config =>
            {
                config.WithMaxConnectionPoolSize(neo4jSettings.MaxConnectionPoolSize)
                      .WithConnectionTimeout(TimeSpan.FromSeconds(neo4jSettings.ConnectionTimeoutSeconds))
                      .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(neo4jSettings.MaxTransactionRetryTimeSeconds));
            });

            return driver;
        });

        // Register Neo4j services
        services.AddScoped<INeo4jDataAccess, Neo4jDataAccess>();
        services.AddScoped<INeo4jReactiveDataAccess, Neo4jReactiveDataAccess>();

        return services;
    }

    /// <summary>
    /// Adds all transaction processing services including processors, pipeline, and background services.
    /// Follows SRP by grouping related application services together.
    /// </summary>
    public static IServiceCollection AddTransactionProcessingServices(this IServiceCollection services)
    {
        // Processors
        services.AddScoped<Neo4jProcessor>();

        // Pipeline
        services.AddScoped<TransactionPipeline>();

        // Background Services
        services.AddHostedService<Neo4jBackgroundService>();

        // Other transaction processing services can be added here
        // Example: services.AddScoped<ITransactionValidator, TransactionValidator>();

        return services;
    }
}