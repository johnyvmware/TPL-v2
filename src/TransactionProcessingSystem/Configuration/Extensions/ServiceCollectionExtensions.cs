using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Components;
using System.Text;
using OpenAI.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration.Validators;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    private const string _telemetrySourceName = "TransactionProcessingSystem";

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
        services.AddTelemetry();
        services.AddDistributedMemoryCache(); // MemoryDistributedCache wraps around MemoryCache, but this lets us get started with the concept of distributed caching;
        services.AddDatabase();
        services.AddFetcher();
        services.AddCategorizer();
        services.AddSingleton<Exporter>();
        services.AddHostedService<Worker>();

        return services;
    }

    private static IServiceCollection AddTelemetry(this IServiceCollection services)
    {
        services
             .AddOpenTelemetry()
             .WithTracing(builder => builder.AddSource(_telemetrySourceName).AddConsoleExporter());

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

    private static void AddCategorizer(this IServiceCollection services)
    {
        services.AddChatClient();
        services.AddCategoriesProvider();
        services.AddCategorizerImplementation();

        services.AddSingleton<AIFunctionService>();
        services.AddSingleton<ICategoryService, CategoryService>();
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
                .UseOpenTelemetry(sourceName: _telemetrySourceName, configure: c => c.EnableSensitiveData = true)
                .Build(serviceProvider);

            return chatClient;
        });
    }

    private static IServiceCollection AddCategoriesProvider(this IServiceCollection services)
    {
        services.AddSingleton<CategoryProviderV2>();
        return services.AddSingleton<ICategoryProvider>(serviceProvider =>
        {
            ILogger<CategoryProvider> logger = serviceProvider.GetRequiredService<ILogger<CategoryProvider>>();
            string categoriesFilePath = serviceProvider.GetRequiredService<IOptions<CategoriesOptions>>().Value.Path;
            string absoluteCategoriesFilePath = Path.Combine(AppContext.BaseDirectory, categoriesFilePath);

            if (!File.Exists(absoluteCategoriesFilePath))
            {
                throw new FileNotFoundException($"Category configuration file not found at: {absoluteCategoriesFilePath}");
            }

            CategoryProvider categoriesProvider = new(absoluteCategoriesFilePath, logger);
            categoriesProvider.Load(); // Maybe this could be move to the pipeline and performed in a async manner

            return categoriesProvider;
        });
    }

    private static IServiceCollection AddCategorizerImplementation(this IServiceCollection services)
    {
        return services.AddSingleton(serviceProvider =>
        {
            var chatClient = serviceProvider.GetRequiredService<IChatClient>();
            var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            var categoriesService = serviceProvider.GetRequiredService<ICategoryService>();
            var aIFunctionService = serviceProvider.GetRequiredService<AIFunctionService>();
            var llmSettings = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;

            return new Categorizer(chatClient, distributedCache, categoriesService, aIFunctionService, llmSettings);
        });
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