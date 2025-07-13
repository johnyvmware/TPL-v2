using DataFlow.Akka;
using DataFlow.Core.Abstractions;
using DataFlow.Examples.Configuration;
using DataFlow.Examples.Processors;
using DataFlow.Examples.Services;
using DataFlow.TPL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataFlow.Examples;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure options
                services.Configure<DataFlowOptions>(
                    context.Configuration.GetSection(DataFlowOptions.SectionName));
                
                // Register processors
                services.AddSingleton<ValidationProcessor>();
                services.AddSingleton<TransformProcessor>();
                services.AddSingleton<LoggingProcessor>();
                
                // Register data flow engine based on configuration
                var dataFlowOptions = context.Configuration
                    .GetSection(DataFlowOptions.SectionName)
                    .Get<DataFlowOptions>() ?? new DataFlowOptions();
                
                RegisterDataFlowEngine(services, dataFlowOptions.Engine);
                
                // Register the main service
                services.AddHostedService<DataFlowService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while running the application");
        }
    }

    private static void RegisterDataFlowEngine(IServiceCollection services, string engineType)
    {
        switch (engineType.ToUpperInvariant())
        {
            case "TPL":
                services.AddSingleton<IDataFlowEngine, TplDataFlowEngine>();
                services.AddSingleton<IConfigurableDataFlowEngine>(provider => 
                    (IConfigurableDataFlowEngine)provider.GetRequiredService<IDataFlowEngine>());
                break;
            
            case "AKKA":
                services.AddSingleton<IDataFlowEngine, AkkaDataFlowEngine>();
                services.AddSingleton<IConfigurableDataFlowEngine>(provider => 
                    (IConfigurableDataFlowEngine)provider.GetRequiredService<IDataFlowEngine>());
                break;
            
            default:
                throw new ArgumentException($"Unknown engine type: {engineType}. Supported types: TPL, Akka");
        }
    }
}