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
    /// Adds and configures Neo4j services with proper validation and dependency injection
    /// </summary>
    public static IServiceCollection AddNeo4jServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Neo4j settings with validation
        services.Configure<Neo4jSettings>(configuration.GetSection("Neo4j"));
        
        // Add validation for the settings
        services.AddSingleton<IValidateOptions<Neo4jSettings>, Neo4jSettingsValidator>();

        // Register Neo4j Driver as singleton with proper configuration
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4jOptions = serviceProvider.GetRequiredService<IOptions<Neo4jSettings>>();
            var settings = neo4jOptions.Value;

            if (!settings.IsValid)
            {
                throw new InvalidOperationException(
                    "Neo4j configuration is invalid. Please check ConnectionUri, Username, and Password.");
            }

            var authToken = AuthTokens.Basic(settings.Username, settings.Password);
            
            var driver = GraphDatabase.Driver(settings.ConnectionUri, authToken, config =>
            {
                config.WithMaxConnectionPoolSize(settings.MaxConnectionPoolSize)
                      .WithConnectionTimeout(TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds))
                      .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(settings.MaxTransactionRetryTimeSeconds));
            });

            return driver;
        });

        return services;
    }
}

/// <summary>
/// Validator for Neo4j settings
/// </summary>
public class Neo4jSettingsValidator : IValidateOptions<Neo4jSettings>
{
    public ValidateOptionsResult Validate(string? name, Neo4jSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionUri))
        {
            return ValidateOptionsResult.Fail("Neo4j ConnectionUri is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return ValidateOptionsResult.Fail("Neo4j Username is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail("Neo4j Password is required.");
        }

        if (!Uri.TryCreate(options.ConnectionUri, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("Neo4j ConnectionUri must be a valid URI.");
        }

        return ValidateOptionsResult.Success;
    }
} 