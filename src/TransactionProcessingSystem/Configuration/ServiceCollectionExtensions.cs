using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Components;
using System.Text;
using OpenAI.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Components.Neo4jExporter;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Extension methods for configuring services following SRP and modern C# practices
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string _sourceName = "TransactionProcessingSystem";

    /// <summary>
    /// Adds and configures all application settings and secrets with validation.
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureAppSettings(services, configuration);
        ConfigureAppSecrets(services, configuration);

        services.AddTelemetry();
         // MemoryDistributedCache wraps around MemoryCache, but this lets us get started with the concept of distributed caching; just evaluate
        services.AddDistributedMemoryCache();

        // Register code pages for Windows-1250 encoding support
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddIChatClient();
        services.AddTransient(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Fetcher>>();
            var settings = serviceProvider.GetRequiredService<IOptions<FetcherOptions>>().Value;
            return new Fetcher(settings, logger);
        });
        services.AddTransient(serviceProvider =>
        {
            var chatClient = serviceProvider.GetRequiredService<IChatClient>();
            var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            var categoriesService = serviceProvider.GetRequiredService<ICategoriesService>();
            var aIFunctionService = serviceProvider.GetRequiredService<AIFunctionService>();
            var llmSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;

            return new Categorizer(chatClient, distributedCache, categoriesService, aIFunctionService, llmSettings);
        });
        //services.AddScoped<TransactionParser>();
        //services.AddScoped<TransactionProcessor>();
        //services.AddScoped<EmailEnricher>();
        //services.AddScoped<Neo4jExporter>();
        services.AddHostedService<Worker>();
        services.AddSingleton(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ExporterV2>>();
            var secrets = serviceProvider.GetRequiredService<IOptions<Neo4jSecrets>>().Value;
            var settings = serviceProvider.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            return new ExporterV2(settings, secrets, logger);
        });

        services.AddSingleton<ICategoriesProvider>(serviceProvider =>
        {
            ILogger<CategoriesProvider> logger = serviceProvider.GetRequiredService<ILogger<CategoriesProvider>>();
            string relativePath = serviceProvider.GetRequiredService<IOptions<CategoriesOptions>>().Value.Path;
            string absolutePath = Path.Combine(AppContext.BaseDirectory, relativePath);

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Category configuration file not found at: {absolutePath}");
            }

            CategoriesProvider categoriesProvider = new(absolutePath, logger);
            categoriesProvider.Load();

            return categoriesProvider;
        });

        services.AddSingleton<ICategoriesService, CategoryService>();
        services.AddSingleton<AIFunctionService>();

        return services;
    }

    private static IServiceCollection AddChatClient(this IServiceCollection services)
    {
        return services.AddSingleton(serviceProvider =>
        {
            var llmSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
            var openAISecrets = serviceProvider.GetRequiredService<IOptions<OpenAISecrets>>().Value;

            return new ChatClient(llmSettings.OpenAI.Model, openAISecrets.ApiKey);
        });
    }

    private static IServiceCollection AddIChatClient(this IServiceCollection services)
    {
        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            OpenAIOptions openAiSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value.OpenAI;
            OpenAISecrets openAISecrets = serviceProvider.GetRequiredService<IOptions<OpenAISecrets>>().Value;
            IChatClient chatClient = new ChatClient(openAiSettings.Model, openAISecrets.ApiKey)
                .AsIChatClient()
                .AsBuilder()
                .UseLogging()
                // MemoryCache configured above
                .UseDistributedCache()
                // This would use logger resolved from container
                .UseFunctionInvocation()
                // ILogging could be a simpler alternative to OpenTelemetry, the console exporter extension writes to console
                .UseOpenTelemetry(sourceName: _sourceName, configure: c => c.EnableSensitiveData = true)
                .Build(serviceProvider);

            return chatClient;
        });

        return services;
    }

    private static void AddTelemetry(this IServiceCollection services)
    {
       services
            .AddOpenTelemetry()
            .WithTracing(builder => builder.AddSource(_sourceName).AddConsoleExporter());
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
        // ValidateOnStart() registers the validation to run when the first service requiring IOptions<T>
        // is resolved, which typically happens during host.RunAsync(). It doesn't validate during the host build phase.

        services
            .AddOptionsWithValidateOnStart<LlmOptions>()
            .Bind(configuration.GetRequiredSection(LlmOptions.SectionName))
            .ValidateDataAnnotations();

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

        services
            .AddOptionsWithValidateOnStart<CategoriesOptions>()
            .Bind(configuration.GetSection(CategoriesOptions.SectionName))
            .ValidateDataAnnotations();
    }
}