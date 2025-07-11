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

        // Configure secrets separately (from User Secrets in dev, Environment Variables in prod)
        services.Configure<SecretsSettings>(configuration);
        services.Configure<OpenAISecrets>(configuration.GetSection("Secrets:OpenAI"));
        services.Configure<MicrosoftGraphSecrets>(configuration.GetSection("Secrets:MicrosoftGraph"));
        services.Configure<Neo4jSecrets>(configuration.GetSection("Secrets:Neo4j"));

        // Configure Neo4j settings separately to follow SRP
        services.Configure<Neo4jSettings>(configuration.GetSection("Neo4j"));

        // Register all validators for comprehensive startup validation
        services.AddSingleton<IValidateOptions<AppSettings>, AppSettingsValidator>();
        services.AddSingleton<IValidateOptions<SecretsSettings>, SecretsSettingsValidator>();
        services.AddSingleton<IValidateOptions<Neo4jSettings>, Neo4jSettingsValidator>();

        return services;
    }

    /// <summary>
    /// Single Neo4j bootstrap call that handles everything Neo4j related.
    /// Follows SRP by keeping all Neo4j concerns together.
    /// </summary>
    public static IServiceCollection AddNeo4jBootstrap(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Neo4jConfiguration factory for direct resolution and IOptions pattern
        services.AddSingleton<Neo4jConfiguration>(serviceProvider =>
        {
            var neo4jSettings = serviceProvider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
            var neo4jSecrets = serviceProvider.GetRequiredService<IOptions<Neo4jSecrets>>().Value;

            return new Neo4jConfiguration
            {
                Settings = neo4jSettings,
                Secrets = neo4jSecrets
            };
        });

        // Register for IOptions<Neo4jConfiguration> pattern using the singleton factory
        services.AddSingleton<IOptions<Neo4jConfiguration>>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<Neo4jConfiguration>();
            return Options.Create(config);
        });

        // Register validator for Neo4jConfiguration
        services.AddSingleton<IValidateOptions<Neo4jConfiguration>, Neo4jConfigurationValidator>();

        // Register Neo4j Driver as singleton with proper configuration
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4jConfig = serviceProvider.GetRequiredService<Neo4jConfiguration>();

            // Validation is handled by IValidateOptions, but we still check IsValid for safety
            if (!neo4jConfig.IsValid)
            {
                throw new InvalidOperationException(
                    "Neo4j configuration is invalid. Please check ConnectionUri, Username, and Password in your secrets configuration.");
            }

            var authToken = AuthTokens.Basic(neo4jConfig.Username, neo4jConfig.Password);

            var driver = GraphDatabase.Driver(neo4jConfig.ConnectionUri, authToken, config =>
            {
                config.WithMaxConnectionPoolSize(neo4jConfig.MaxConnectionPoolSize)
                      .WithConnectionTimeout(TimeSpan.FromSeconds(neo4jConfig.ConnectionTimeoutSeconds))
                      .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(neo4jConfig.MaxTransactionRetryTimeSeconds));
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