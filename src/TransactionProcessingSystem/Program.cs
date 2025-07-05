using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Pipeline;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        using var host = builder.Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Transaction Processing System");

        try
        {
            // Start the mock API service
            var mockApiService = host.Services.GetRequiredService<MockTransactionApiService>();
            await host.StartAsync();

            // Wait a moment for the API to start
            await Task.Delay(2000);

            // Run the transaction processing pipeline
            var pipeline = host.Services.GetRequiredService<TransactionPipeline>();
            var result = await pipeline.ProcessTransactionsAsync("api/transactions");

            if (result.Success)
            {
                logger.LogInformation("Transaction processing completed successfully in {Duration}", result.Duration);
            }
            else
            {
                logger.LogError("Transaction processing failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during transaction processing");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var configuration = context.Configuration;

        // Configuration
        services.Configure<AppSettings>(configuration);
        services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
        services.Configure<MicrosoftGraphSettings>(configuration.GetSection("MicrosoftGraph"));
        services.Configure<TransactionApiSettings>(configuration.GetSection("TransactionApi"));
        services.Configure<ExportSettings>(configuration.GetSection("Export"));
        services.Configure<PipelineSettings>(configuration.GetSection("Pipeline"));

        // Bind configuration objects
        var openAISettings = configuration.GetSection("OpenAI").Get<OpenAISettings>() ?? new OpenAISettings { ApiKey = "test-key" };
        var graphSettings = configuration.GetSection("MicrosoftGraph").Get<MicrosoftGraphSettings>() ?? new MicrosoftGraphSettings 
        { 
            ClientId = "test-client-id", 
            ClientSecret = "test-client-secret", 
            TenantId = "test-tenant-id" 
        };
        var transactionApiSettings = configuration.GetSection("TransactionApi").Get<TransactionApiSettings>() ?? new TransactionApiSettings 
        { 
            BaseUrl = "http://localhost:5000" 
        };
        var exportSettings = configuration.GetSection("Export").Get<ExportSettings>() ?? new ExportSettings 
        { 
            OutputDirectory = "./output" 
        };
        var pipelineSettings = configuration.GetSection("Pipeline").Get<PipelineSettings>() ?? new PipelineSettings();

        // HTTP Client
        services.AddHttpClient<TransactionFetcherAgent>();

        // Agents
        services.AddTransient<TransactionFetcherAgent>(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            var logger = provider.GetRequiredService<ILogger<TransactionFetcherAgent>>();
            return new TransactionFetcherAgent(httpClient, transactionApiSettings, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<TransactionProcessorAgent>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TransactionProcessorAgent>>();
            return new TransactionProcessorAgent(logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<EmailEnricherAgent>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<EmailEnricherAgent>>();
            return new EmailEnricherAgent(graphSettings, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<CategorizerAgent>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CategorizerAgent>>();
            return new CategorizerAgent(openAISettings, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<CsvExporterAgent>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CsvExporterAgent>>();
            return new CsvExporterAgent(exportSettings, logger, pipelineSettings.BoundedCapacity);
        });

        // Pipeline
        services.AddTransient<TransactionPipeline>(provider =>
        {
            var fetcherAgent = provider.GetRequiredService<TransactionFetcherAgent>();
            var processorAgent = provider.GetRequiredService<TransactionProcessorAgent>();
            var enricherAgent = provider.GetRequiredService<EmailEnricherAgent>();
            var categorizerAgent = provider.GetRequiredService<CategorizerAgent>();
            var exporterAgent = provider.GetRequiredService<CsvExporterAgent>();
            var logger = provider.GetRequiredService<ILogger<TransactionPipeline>>();
            
            return new TransactionPipeline(
                fetcherAgent,
                processorAgent,
                enricherAgent,
                categorizerAgent,
                exporterAgent,
                pipelineSettings,
                logger);
        });

        // Services
        services.AddSingleton<MockTransactionApiService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<MockTransactionApiService>>();
            return new MockTransactionApiService(logger, transactionApiSettings.BaseUrl);
        });

        services.AddHostedService<MockTransactionApiService>(provider => 
            provider.GetRequiredService<MockTransactionApiService>());
    }
}