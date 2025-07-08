using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using TransactionProcessingSystem.Components;
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
            // Initialize Neo4j database
            await InitializeNeo4jAsync(host.Services, logger);

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
        services.Configure<Neo4jSettings>(configuration.GetSection("Neo4j"));

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
        var neo4jSettings = configuration.GetSection("Neo4j").Get<Neo4jSettings>() ?? new Neo4jSettings
        {
            ConnectionUri = "neo4j+s://demo.neo4jlabs.com",
            Username = "demo",
            Password = "demo"
        };

        // Configure Neo4j Driver (Singleton pattern as recommended by Neo4j)
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Configuring Neo4j driver for URI: {Uri}", neo4jSettings.ConnectionUri);

            var configBuilder = Config.Builder
                .WithMaxConnectionPoolSize(neo4jSettings.MaxConnectionPoolSize)
                .WithConnectionTimeout(TimeSpan.FromSeconds(neo4jSettings.ConnectionTimeoutSeconds))
                .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(neo4jSettings.MaxTransactionRetryTimeSeconds))
                .WithTrustStrategy(TrustStrategy.TrustSystemCaSignedCertificates);

            if (neo4jSettings.EnableMetrics)
            {
                configBuilder = configBuilder.WithDriverMetrics();
            }

            var config = configBuilder.ToConfig();

            try
            {
                var driver = GraphDatabase.Driver(
                    neo4jSettings.ConnectionUri,
                    AuthTokens.Basic(neo4jSettings.Username, neo4jSettings.Password),
                    config);

                logger.LogInformation("Neo4j driver configured successfully");
                return driver;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Neo4j driver");
                throw;
            }
        });

        // Configure Neo4j services
        services.AddScoped<INeo4jDataAccess, Neo4jDataAccess>();

        // HTTP Client
        services.AddHttpClient<TransactionFetcher>();

        // Components
        services.AddTransient<TransactionFetcher>(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            var logger = provider.GetRequiredService<ILogger<TransactionFetcher>>();
            return new TransactionFetcher(httpClient, transactionApiSettings, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<TransactionProcessor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TransactionProcessor>>();
            return new TransactionProcessor(logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<EmailEnricher>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<EmailEnricher>>();
            return new EmailEnricher(graphSettings, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<Categorizer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Categorizer>>();
            return new Categorizer(openAISettings, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<Neo4jProcessor>(provider =>
        {
            var neo4jDataAccess = provider.GetRequiredService<INeo4jDataAccess>();
            var logger = provider.GetRequiredService<ILogger<Neo4jProcessor>>();
            return new Neo4jProcessor(neo4jDataAccess, logger, pipelineSettings.BoundedCapacity);
        });

        services.AddTransient<CsvExporter>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CsvExporter>>();
            return new CsvExporter(exportSettings, logger, pipelineSettings.BoundedCapacity);
        });

        // Pipeline
        services.AddTransient<TransactionPipeline>(provider =>
        {
            var fetcher = provider.GetRequiredService<TransactionFetcher>();
            var processor = provider.GetRequiredService<TransactionProcessor>();
            var enricher = provider.GetRequiredService<EmailEnricher>();
            var categorizer = provider.GetRequiredService<Categorizer>();
            var neo4jProcessor = provider.GetRequiredService<Neo4jProcessor>();
            var exporter = provider.GetRequiredService<CsvExporter>();
            var logger = provider.GetRequiredService<ILogger<TransactionPipeline>>();

            return new TransactionPipeline(
                fetcher,
                processor,
                enricher,
                categorizer,
                neo4jProcessor,
                exporter,
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

    private static async Task InitializeNeo4jAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var neo4jDataAccess = scope.ServiceProvider.GetService<INeo4jDataAccess>();
        
        if (neo4jDataAccess == null)
        {
            logger.LogWarning("Neo4j data access service not available. Skipping Neo4j initialization.");
            return;
        }
        
        try
        {
            logger.LogInformation("Initializing Neo4j database...");
            
            // Verify connectivity
            var isConnected = await neo4jDataAccess.VerifyConnectivityAsync();
            if (!isConnected)
            {
                logger.LogWarning("Neo4j connectivity check failed. The application will continue but graph features may not work.");
                return;
            }

            // Create indexes for better performance
            await neo4jDataAccess.CreateIndexesAsync();
            
            logger.LogInformation("Neo4j database initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Neo4j database. The application will continue but graph features may not work.");
        }
    }
}