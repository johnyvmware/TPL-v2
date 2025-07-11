using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Validator for OpenAI settings
/// </summary>
public sealed class OpenAISettingsValidator : IValidateOptions<OpenAISettings>
{
    public ValidateOptionsResult Validate(string? name, OpenAISettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Model))
            failures.Add("OpenAI.Model is required");

        if (options.MaxTokens <= 0)
            failures.Add("OpenAI.MaxTokens must be greater than 0");

        if (options.Temperature < 0 || options.Temperature > 2)
            failures.Add("OpenAI.Temperature must be between 0 and 2");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Microsoft Graph settings
/// </summary>
public sealed class MicrosoftGraphSettingsValidator : IValidateOptions<MicrosoftGraphSettings>
{
    public ValidateOptionsResult Validate(string? name, MicrosoftGraphSettings options)
    {
        var failures = new List<string>();

        if (options.EmailSearchDays <= 0)
            failures.Add("MicrosoftGraph.EmailSearchDays must be greater than 0");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Transaction API settings
/// </summary>
public sealed class TransactionApiSettingsValidator : IValidateOptions<TransactionApiSettings>
{
    public ValidateOptionsResult Validate(string? name, TransactionApiSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            failures.Add("TransactionApi.BaseUrl is required");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            failures.Add("TransactionApi.BaseUrl must be a valid URI");

        if (options.TimeoutSeconds <= 0)
            failures.Add("TransactionApi.TimeoutSeconds must be greater than 0");

        if (options.MaxRetries < 0)
            failures.Add("TransactionApi.MaxRetries must be 0 or greater");

        if (options.BatchSize <= 0)
            failures.Add("TransactionApi.BatchSize must be greater than 0");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Export settings
/// </summary>
public sealed class ExportSettingsValidator : IValidateOptions<ExportSettings>
{
    public ValidateOptionsResult Validate(string? name, ExportSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            failures.Add("Export.OutputDirectory is required");

        if (string.IsNullOrWhiteSpace(options.FileNameFormat))
            failures.Add("Export.FileNameFormat is required");

        if (options.BufferSize <= 0)
            failures.Add("Export.BufferSize must be greater than 0");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Pipeline settings
/// </summary>
public sealed class PipelineSettingsValidator : IValidateOptions<PipelineSettings>
{
    public ValidateOptionsResult Validate(string? name, PipelineSettings options)
    {
        var failures = new List<string>();

        if (options.BoundedCapacity <= 0)
            failures.Add("Pipeline.BoundedCapacity must be greater than 0");

        if (options.MaxDegreeOfParallelism <= 0)
            failures.Add("Pipeline.MaxDegreeOfParallelism must be greater than 0");

        if (options.TimeoutMinutes <= 0)
            failures.Add("Pipeline.TimeoutMinutes must be greater than 0");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Neo4j settings
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
/// Validator for AppSettings configuration that handles all nested settings gracefully
/// </summary>
public sealed class AppSettingsValidator : IValidateOptions<AppSettings>
{
    private readonly IValidateOptions<OpenAISettings> _openAIValidator;
    private readonly IValidateOptions<MicrosoftGraphSettings> _microsoftGraphValidator;
    private readonly IValidateOptions<TransactionApiSettings> _transactionApiValidator;
    private readonly IValidateOptions<ExportSettings> _exportValidator;
    private readonly IValidateOptions<PipelineSettings> _pipelineValidator;
    private readonly IValidateOptions<Neo4jSettings> _neo4jValidator;

    public AppSettingsValidator(
        IValidateOptions<OpenAISettings> openAIValidator,
        IValidateOptions<MicrosoftGraphSettings> microsoftGraphValidator,
        IValidateOptions<TransactionApiSettings> transactionApiValidator,
        IValidateOptions<ExportSettings> exportValidator,
        IValidateOptions<PipelineSettings> pipelineValidator,
        IValidateOptions<Neo4jSettings> neo4jValidator)
    {
        _openAIValidator = openAIValidator;
        _microsoftGraphValidator = microsoftGraphValidator;
        _transactionApiValidator = transactionApiValidator;
        _exportValidator = exportValidator;
        _pipelineValidator = pipelineValidator;
        _neo4jValidator = neo4jValidator;
    }

    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        var failures = new List<string>();

        // Validate OpenAI settings
        var openAIResult = _openAIValidator.Validate(name, options.OpenAI);
        if (openAIResult.Failed)
            failures.AddRange(openAIResult.Failures);

        // Validate Microsoft Graph settings
        var microsoftGraphResult = _microsoftGraphValidator.Validate(name, options.MicrosoftGraph);
        if (microsoftGraphResult.Failed)
            failures.AddRange(microsoftGraphResult.Failures);

        // Validate Transaction API settings
        var transactionApiResult = _transactionApiValidator.Validate(name, options.TransactionApi);
        if (transactionApiResult.Failed)
            failures.AddRange(transactionApiResult.Failures);

        // Validate Export settings
        var exportResult = _exportValidator.Validate(name, options.Export);
        if (exportResult.Failed)
            failures.AddRange(exportResult.Failures);

        // Validate Pipeline settings
        var pipelineResult = _pipelineValidator.Validate(name, options.Pipeline);
        if (pipelineResult.Failed)
            failures.AddRange(pipelineResult.Failures);

        // Validate Neo4j settings
        var neo4jResult = _neo4jValidator.Validate(name, options.Neo4j);
        if (neo4jResult.Failed)
            failures.AddRange(neo4jResult.Failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for OpenAI secrets
/// </summary>
public sealed class OpenAISecretsValidator : IValidateOptions<OpenAISecrets>
{
    public ValidateOptionsResult Validate(string? name, OpenAISecrets options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            failures.Add("OpenAI.ApiKey is required in secrets configuration");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Microsoft Graph secrets
/// </summary>
public sealed class MicrosoftGraphSecretsValidator : IValidateOptions<MicrosoftGraphSecrets>
{
    public ValidateOptionsResult Validate(string? name, MicrosoftGraphSecrets options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ClientId))
            failures.Add("MicrosoftGraph.ClientId is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            failures.Add("MicrosoftGraph.ClientSecret is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.TenantId))
            failures.Add("MicrosoftGraph.TenantId is required in secrets configuration");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validator for Neo4j secrets
/// </summary>
public sealed class Neo4jSecretsValidator : IValidateOptions<Neo4jSecrets>
{
    public ValidateOptionsResult Validate(string? name, Neo4jSecrets options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionUri))
            failures.Add("Neo4j.ConnectionUri is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add("Neo4j.Username is required in secrets configuration");

        if (string.IsNullOrWhiteSpace(options.Password))
            failures.Add("Neo4j.Password is required in secrets configuration");

        // Validate Neo4j connection URI format
        if (!string.IsNullOrWhiteSpace(options.ConnectionUri))
        {
            if (!Uri.TryCreate(options.ConnectionUri, UriKind.Absolute, out var uri))
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
/// Validator for SecretsSettings configuration that handles all nested secrets gracefully
/// </summary>
public sealed class SecretsSettingsValidator : IValidateOptions<SecretsSettings>
{
    private readonly IValidateOptions<OpenAISecrets> _openAISecretsValidator;
    private readonly IValidateOptions<MicrosoftGraphSecrets> _microsoftGraphSecretsValidator;
    private readonly IValidateOptions<Neo4jSecrets> _neo4jSecretsValidator;

    public SecretsSettingsValidator(
        IValidateOptions<OpenAISecrets> openAISecretsValidator,
        IValidateOptions<MicrosoftGraphSecrets> microsoftGraphSecretsValidator,
        IValidateOptions<Neo4jSecrets> neo4jSecretsValidator)
    {
        _openAISecretsValidator = openAISecretsValidator;
        _microsoftGraphSecretsValidator = microsoftGraphSecretsValidator;
        _neo4jSecretsValidator = neo4jSecretsValidator;
    }

    public ValidateOptionsResult Validate(string? name, SecretsSettings options)
    {
        var failures = new List<string>();

        // Validate OpenAI secrets
        var openAIResult = _openAISecretsValidator.Validate(name, options.OpenAI);
        if (openAIResult.Failed)
            failures.AddRange(openAIResult.Failures);

        // Validate Microsoft Graph secrets
        var microsoftGraphResult = _microsoftGraphSecretsValidator.Validate(name, options.MicrosoftGraph);
        if (microsoftGraphResult.Failed)
            failures.AddRange(microsoftGraphResult.Failures);

        // Validate Neo4j secrets
        var neo4jResult = _neo4jSecretsValidator.Validate(name, options.Neo4j);
        if (neo4jResult.Failed)
            failures.AddRange(neo4jResult.Failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}