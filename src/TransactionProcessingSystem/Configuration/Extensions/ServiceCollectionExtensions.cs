using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Configuration.Secrets;
using TransactionProcessingSystem.Configuration.Settings;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    private const string TelemetrySourceName = "TransactionProcessingSystem";

    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureSettings(services, configuration);
        ConfigureSecrets(services, configuration);

        // Register code pages for Windows-1250 encoding support
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTelemetry();
        services.AddDistributedMemoryCache(); // MemoryDistributedCache wraps around MemoryCache, but this lets us get started with the concept of distributed caching;
        services.AddDatabase();
        services.AddFetcher();
        services.AddCategorizer();
        services.AddEnricher();
        services.AddSingleton<Exporter>();
        services.AddHostedService<Worker>();

        return services;
    }

    private static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services
             .AddOpenTelemetry()
             .WithTracing(builder => builder.AddSource(TelemetrySourceName).AddConsoleExporter());

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        return services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var secrets = serviceProvider.GetRequiredService<IOptions<Neo4jSecrets>>().Value;
            var settings = serviceProvider.GetRequiredService<IOptions<Neo4jOptions>>().Value;

            return new DatabaseService(settings, secrets);
        });
    }

    private static IServiceCollection AddFetcher(this IServiceCollection services)
    {
        return services.AddSingleton(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Fetcher>>();
            var settings = serviceProvider.GetRequiredService<IOptions<FetcherOptions>>().Value;
            return new Fetcher(settings, logger);
        });
    }

    private static IServiceCollection AddEnricher(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphOptions>>().Value;
            var secrets = serviceProvider.GetRequiredService<IOptions<MicrosoftGraphSecrets>>().Value;
            return new MicrosoftGraphService(settings, secrets);
        });

        return services.AddSingleton<Enricher>();
    }

    private static void AddCategorizer(this IServiceCollection services)
    {
        services.AddChatClient();
        services.AddSingleton<CategoryProvider>();
        services.AddCategorizerImplementation();

        services.AddSingleton<AIFunctionService>();
        services.AddSingleton<ICategoryValidator, CategoryService>();
    }

    private static IServiceCollection AddChatClient(this IServiceCollection services)
    {
        return services.AddSingleton(serviceProvider =>
        {
            OpenAIOptions openAiSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value.OpenAI;
            OpenAISecrets openAISecrets = serviceProvider.GetRequiredService<IOptions<OpenAISecrets>>().Value;
            IChatClient chatClient = new ChatClient(openAiSettings.Model, openAISecrets.ApiKey)
                .AsIChatClient()
                .AsBuilder()
                .UseLogging()
                .UseDistributedCache() // MemoryCache configured in the AddApplicationServices method
                .UseFunctionInvocation()
                .UseOpenTelemetry(sourceName: TelemetrySourceName, configure: c => c.EnableSensitiveData = true)
                .Build(serviceProvider);

            return chatClient;
        });
    }

    private static IServiceCollection AddCategorizerImplementation(this IServiceCollection services)
    {
        return services.AddSingleton(serviceProvider =>
        {
            var chatClient = serviceProvider.GetRequiredService<IChatClient>();
            var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            var categoriesService = serviceProvider.GetRequiredService<ICategoryValidator>();
            var aIFunctionService = serviceProvider.GetRequiredService<AIFunctionService>();
            var llmSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;

            return new Categorizer(chatClient, distributedCache, categoriesService, aIFunctionService, llmSettings);
        });
    }

    private static void ConfigureSecrets(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptionsWithValidateOnStart<OpenAISecrets>()
            .Bind(configuration.GetRequiredSection(OpenAISecrets.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jSecrets>()
            .Bind(configuration.GetRequiredSection(Neo4jSecrets.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphSecrets>()
            .Bind(configuration.GetRequiredSection(MicrosoftGraphSecrets.SectionName))
            .ValidateDataAnnotations();
    }

    private static void ConfigureSettings(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptionsWithValidateOnStart<LlmOptions>()
            .Bind(configuration.GetRequiredSection(LlmOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<Neo4jOptions>()
            .Bind(configuration.GetSection(Neo4jOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<FetcherOptions>()
            .Bind(configuration.GetSection(FetcherOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<MicrosoftGraphOptions>()
            .Bind(configuration.GetSection(MicrosoftGraphOptions.SectionName))
            .ValidateDataAnnotations();
    }
}
