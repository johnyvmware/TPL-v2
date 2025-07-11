using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Validator for AppSettings configuration
/// </summary>
public sealed class AppSettingsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        var failures = new List<string>();

        // Validate OpenAI settings
        if (string.IsNullOrWhiteSpace(options.OpenAI.Model))
            failures.Add("OpenAI.Model is required");

        if (options.OpenAI.MaxTokens <= 0)
            failures.Add("OpenAI.MaxTokens must be greater than 0");

        if (options.OpenAI.Temperature < 0 || options.OpenAI.Temperature > 2)
            failures.Add("OpenAI.Temperature must be between 0 and 2");

        // Validate Microsoft Graph settings
        if (options.MicrosoftGraph.EmailSearchDays <= 0)
            failures.Add("MicrosoftGraph.EmailSearchDays must be greater than 0");

        // Validate Transaction API settings
        if (string.IsNullOrWhiteSpace(options.TransactionApi.BaseUrl))
            failures.Add("TransactionApi.BaseUrl is required");

        if (!Uri.TryCreate(options.TransactionApi.BaseUrl, UriKind.Absolute, out _))
            failures.Add("TransactionApi.BaseUrl must be a valid URI");

        if (options.TransactionApi.TimeoutSeconds <= 0)
            failures.Add("TransactionApi.TimeoutSeconds must be greater than 0");

        if (options.TransactionApi.MaxRetries < 0)
            failures.Add("TransactionApi.MaxRetries must be 0 or greater");

        if (options.TransactionApi.BatchSize <= 0)
            failures.Add("TransactionApi.BatchSize must be greater than 0");

        // Validate Export settings
        if (string.IsNullOrWhiteSpace(options.Export.OutputDirectory))
            failures.Add("Export.OutputDirectory is required");

        if (string.IsNullOrWhiteSpace(options.Export.FileNameFormat))
            failures.Add("Export.FileNameFormat is required");

        if (options.Export.BufferSize <= 0)
            failures.Add("Export.BufferSize must be greater than 0");

        // Validate Pipeline settings
        if (options.Pipeline.BoundedCapacity <= 0)
            failures.Add("Pipeline.BoundedCapacity must be greater than 0");

        if (options.Pipeline.MaxDegreeOfParallelism <= 0)
            failures.Add("Pipeline.MaxDegreeOfParallelism must be greater than 0");

        if (options.Pipeline.TimeoutMinutes <= 0)
            failures.Add("Pipeline.TimeoutMinutes must be greater than 0");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for SecretsSettings configuration
/// </summary>
public sealed class SecretsSettingsValidator : IValidateOptions<SecretsSettings>
{
    public ValidateOptionsResult Validate(string? name, SecretsSettings options)
    {
        var failures = new List<string>();

        // Validate OpenAI secrets
        if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
            failures.Add("OpenAI.ApiKey is required in secrets configuration");

        // Validate Microsoft Graph secrets
        if (string.IsNullOrWhiteSpace(options.MicrosoftGraph.ClientId))
            failures.Add("MicrosoftGraph.ClientId is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.MicrosoftGraph.ClientSecret))
            failures.Add("MicrosoftGraph.ClientSecret is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.MicrosoftGraph.TenantId))
            failures.Add("MicrosoftGraph.TenantId is required in secrets configuration");

        // Validate Neo4j secrets
        if (string.IsNullOrWhiteSpace(options.Neo4j.ConnectionUri))
            failures.Add("Neo4j.ConnectionUri is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Neo4j.Username))
            failures.Add("Neo4j.Username is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Neo4j.Password))
            failures.Add("Neo4j.Password is required in secrets configuration");

        // Validate Neo4j connection URI format
        if (!string.IsNullOrWhiteSpace(options.Neo4j.ConnectionUri))
        {
            if (!Uri.TryCreate(options.Neo4j.ConnectionUri, UriKind.Absolute, out var uri))
            {
                failures.Add("Neo4j.ConnectionUri must be a valid URI");
            }
            else if (!IsValidNeo4jScheme(uri.Scheme))
            {
                failures.Add("Neo4j.ConnectionUri must use a valid Neo4j scheme (bolt, bolt+s, bolt+ssc, neo4j, neo4j+s, neo4j+ssc)");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidNeo4jScheme(string scheme) =>
        scheme.ToLowerInvariant() switch
        {
            "bolt" or "bolt+s" or "bolt+ssc" or "neo4j" or "neo4j+s" or "neo4j+ssc" => true,
            _ => false
        };
}

/// <summary>
/// Validator for Neo4jSettings configuration
/// </summary>
public sealed class Neo4jSettingsValidator : IValidateOptions<Neo4jSettings>
{
    public ValidateOptionsResult Validate(string? name, Neo4jSettings options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            var failures = results.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Neo4jConfiguration (combined settings and secrets)
/// </summary>
public sealed class Neo4jConfigurationValidator : IValidateOptions<Neo4jConfiguration>
{
    public ValidateOptionsResult Validate(string? name, Neo4jConfiguration options)
    {
        var failures = new List<string>();

        // Validate that both settings and secrets are provided
        if (options.Settings == null)
            failures.Add("Neo4j Settings are required");

        if (options.Secrets == null)
            failures.Add("Neo4j Secrets are required");

        if (failures.Count > 0)
            return ValidateOptionsResult.Fail(failures);

        // Use the IsValid property for comprehensive validation
        if (!options.IsValid)
        {
            failures.Add("Neo4j configuration validation failed. Check ConnectionUri, Username, Password, and Database values");
        }

        // Additional specific validations
        if (string.IsNullOrWhiteSpace(options.ConnectionUri))
            failures.Add("Neo4j ConnectionUri is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add("Neo4j Username is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Password))
            failures.Add("Neo4j Password is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Database))
            failures.Add("Neo4j Database is required in settings configuration");

        // Validate URI format
        if (!string.IsNullOrWhiteSpace(options.ConnectionUri))
        {
            if (!Uri.TryCreate(options.ConnectionUri, UriKind.Absolute, out var uri))
            {
                failures.Add("Neo4j ConnectionUri must be a valid absolute URI");
            }
            else
            {
                var validSchemes = new[] { "bolt", "bolt+s", "bolt+ssc", "neo4j", "neo4j+s", "neo4j+ssc" };
                if (!validSchemes.Contains(uri.Scheme.ToLowerInvariant()))
                {
                    failures.Add($"Neo4j ConnectionUri must use a valid scheme: {string.Join(", ", validSchemes)}");
                }
            }
        }

        // Validate numeric ranges
        if (options.MaxConnectionPoolSize <= 0 || options.MaxConnectionPoolSize > 1000)
            failures.Add("Neo4j MaxConnectionPoolSize must be between 1 and 1000");

        if (options.ConnectionTimeoutSeconds <= 0 || options.ConnectionTimeoutSeconds > 300)
            failures.Add("Neo4j ConnectionTimeoutSeconds must be between 1 and 300");

        if (options.MaxTransactionRetryTimeSeconds <= 0 || options.MaxTransactionRetryTimeSeconds > 300)
            failures.Add("Neo4j MaxTransactionRetryTimeSeconds must be between 1 and 300");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}