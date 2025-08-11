using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Components;
using System.Text;
using OpenAI.Chat;
using OpenAI.Responses;

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

        // Register code pages for Windows-1250 encoding support
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddChatClient();

        services.AddTransient<Fetcher>();
        services.AddTransient<Categorizer>();
        //services.AddScoped<TransactionParser>();
        //services.AddScoped<TransactionProcessor>();
        //services.AddScoped<EmailEnricher>();
        //services.AddScoped<Neo4jExporter>();
        services.AddHostedService<Worker>();
        return services;
    }

    private static IServiceCollection AddChatClient(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var llmSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
            var openAISecrets = serviceProvider.GetRequiredService<IOptions<OpenAISecrets>>().Value;

            return new ChatClient(llmSettings.OpenAI.Model, openAISecrets.ApiKey);
        });

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
            var neo4jSettings = serviceProvider.GetRequiredService<IOptions<Neo4jOptions>>().Value;
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

    private static void ConfigureAppSecrets(IServiceCollection services, IConfiguration configuration)
    {
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
/*         services
            .AddOptionsWithValidateOnStart<LlmOptions>()
            .Bind(configuration.GetSection(nameof(LlmOptions)))
            .ValidateDataAnnotations(); */

        services
            .AddOptions<LlmOptions>()
            .Bind(configuration.GetSection(LlmOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphOptions>()
            .Bind(configuration.GetSection("MicrosoftGraph"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<ExportOptions>()
            .Bind(configuration.GetSection("Export"))
            .ValidateDataAnnotations();

        services
            .AddSingleton<IValidateOptions<PipelineOptions>, MaxDegreeOfParallelismValidator>()
            .AddOptionsWithValidateOnStart<PipelineOptions>()
            .Bind(configuration.GetSection("Pipeline"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jOptions>()
            .Bind(configuration.GetSection("Neo4j"))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<FetcherOptions>()
            .Bind(configuration.GetSection("TransactionFetcher"))
            .ValidateDataAnnotations();
    }
}