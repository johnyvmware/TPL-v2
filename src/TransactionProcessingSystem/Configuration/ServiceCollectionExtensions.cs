using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Extension methods for configuring services related to application settings
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures all application settings including secrets
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure regular application settings
        services.Configure<AppSettings>(configuration);
        services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
        services.Configure<MicrosoftGraphSettings>(configuration.GetSection("MicrosoftGraph"));
        services.Configure<TransactionApiSettings>(configuration.GetSection("TransactionApi"));
        services.Configure<ExportSettings>(configuration.GetSection("Export"));
        services.Configure<PipelineSettings>(configuration.GetSection("Pipeline"));
        services.Configure<Neo4jSettings>(configuration.GetSection("Neo4j"));

        // Configure secrets (from User Secrets in dev, Environment Variables in prod)
        services.Configure<SecretsSettings>(configuration);
        services.Configure<OpenAISecrets>(configuration.GetSection("Secrets:OpenAI"));
        services.Configure<MicrosoftGraphSecrets>(configuration.GetSection("Secrets:MicrosoftGraph"));
        services.Configure<Neo4jSecrets>(configuration.GetSection("Secrets:Neo4j"));

        // Create combined Neo4j configuration
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

        return services;
    }

    /// <summary>
    /// Adds and configures Neo4j services with proper validation and dependency injection
    /// </summary>
    public static IServiceCollection AddNeo4jServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add validation for the configuration
        services.AddSingleton<IValidateOptions<Neo4jConfiguration>, Neo4jConfigurationValidator>();

        // Register Neo4j Driver as singleton with proper configuration
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4jConfig = serviceProvider.GetRequiredService<Neo4jConfiguration>();

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

        return services;
    }
}

/// <summary>
/// Validator for Neo4j configuration
/// </summary>
public class Neo4jConfigurationValidator : IValidateOptions<Neo4jConfiguration>
{
    public ValidateOptionsResult Validate(string? name, Neo4jConfiguration options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionUri))
        {
            return ValidateOptionsResult.Fail("Neo4j ConnectionUri is required in secrets configuration.");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return ValidateOptionsResult.Fail("Neo4j Username is required in secrets configuration.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail("Neo4j Password is required in secrets configuration.");
        }

        if (!Uri.TryCreate(options.ConnectionUri, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("Neo4j ConnectionUri must be a valid URI.");
        }

        return ValidateOptionsResult.Success;
    }
}