using DataFlow.Core.Abstractions;
using DataFlow.Core.Models;
using DataFlow.Examples.Configuration;
using DataFlow.Examples.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataFlow.Examples.Services;

/// <summary>
/// Background service that manages the data flow engine
/// </summary>
public class DataFlowService : BackgroundService
{
    private readonly IDataFlowEngine _engine;
    private readonly DataFlowOptions _options;
    private readonly ILogger<DataFlowService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DataFlowService(
        IDataFlowEngine engine,
        IOptions<DataFlowOptions> options,
        ILogger<DataFlowService> logger,
        IServiceProvider serviceProvider)
    {
        _engine = engine;
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting DataFlow service with {EngineType} engine", _options.Engine);
        
        // Configure the engine if it supports configuration
        if (_engine is IConfigurableDataFlowEngine configurableEngine)
        {
            configurableEngine.Configure(_options.Configuration);
        }
        
        // Register processors
        RegisterProcessors();
        
        // Start the engine
        await _engine.StartAsync(cancellationToken);
        
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping DataFlow service");
        
        await _engine.StopAsync(cancellationToken);
        
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataFlow service is running");
        
        // Send some test messages
        await SendTestMessages(stoppingToken);
        
        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private void RegisterProcessors()
    {
        _logger.LogInformation("Registering processors");
        
        foreach (var processorConfig in _options.Processors.Where(p => p.Enabled))
        {
            var processor = CreateProcessor(processorConfig);
            if (processor != null)
            {
                _engine.AddProcessor(processor);
                _logger.LogInformation("Registered processor {ProcessorId} of type {ProcessorType}", 
                    processorConfig.Id, processorConfig.Type);
            }
        }
    }

    private IDataFlowProcessor? CreateProcessor(ProcessorConfiguration config)
    {
        return config.Type switch
        {
            "ValidationProcessor" => _serviceProvider.GetService<ValidationProcessor>(),
            "TransformProcessor" => _serviceProvider.GetService<TransformProcessor>(),
            "LoggingProcessor" => _serviceProvider.GetService<LoggingProcessor>(),
            _ => null
        };
    }

    private async Task SendTestMessages(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending test messages");
        
        var testMessages = new[]
        {
            "Hello, World!",
            "This is a test message",
            "Another test message",
            "Final test message"
        };
        
        foreach (var messageText in testMessages)
        {
            var message = new DataFlowMessage<string>
            {
                Payload = messageText,
                Properties = new Dictionary<string, object>
                {
                    ["Source"] = "DataFlowService",
                    ["MessageType"] = "TestMessage"
                }
            };
            
            await _engine.SendAsync(message, cancellationToken);
            _logger.LogInformation("Sent test message: {Message}", messageText);
            
            // Add some delay between messages
            await Task.Delay(1000, cancellationToken);
        }
    }
}