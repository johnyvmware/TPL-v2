using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Configuration;
using DataFlow.Akka.Actors;
using DataFlow.Core.Abstractions;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Akka;

/// <summary>
/// Akka.NET implementation of the data flow engine
/// </summary>
public class AkkaDataFlowEngine : IConfigurableDataFlowEngine
{
    private readonly ILogger<AkkaDataFlowEngine> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IDataFlowProcessor> _processors = new();
    private readonly ConcurrentDictionary<string, IActorRef> _processorActors = new();
    private ActorSystem? _actorSystem;
    private IActorRef? _coordinatorActor;
    private DataFlowConfiguration _configuration = new();

    public AkkaDataFlowEngine(ILogger<AkkaDataFlowEngine> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public void Configure(DataFlowConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("Akka.NET DataFlow engine configured with MaxConcurrency: {MaxConcurrency}, BufferSize: {BufferSize}", 
            _configuration.MaxConcurrency, _configuration.BufferSize);
    }

    public void AddProcessor(IDataFlowProcessor processor)
    {
        if (_processors.TryAdd(processor.Id, processor))
        {
            _logger.LogInformation("Added processor {ProcessorId} to Akka.NET DataFlow engine", processor.Id);
            
            // Create actor for processor if system is running
            if (_actorSystem != null)
            {
                CreateProcessorActor(processor);
            }
        }
        else
        {
            _logger.LogWarning("Processor {ProcessorId} already exists in Akka.NET DataFlow engine", processor.Id);
        }
    }

    public bool RemoveProcessor(string processorId)
    {
        if (_processors.TryRemove(processorId, out var processor))
        {
            _logger.LogInformation("Removed processor {ProcessorId} from Akka.NET DataFlow engine", processorId);
            
            // Stop and remove actor
            if (_processorActors.TryRemove(processorId, out var actor))
            {
                _actorSystem?.Stop(actor);
            }
            
            return true;
        }

        _logger.LogWarning("Processor {ProcessorId} not found in Akka.NET DataFlow engine", processorId);
        return false;
    }

    public async Task SendAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default)
    {
        if (_coordinatorActor == null)
        {
            throw new InvalidOperationException("Engine not started. Call StartAsync first.");
        }

        _logger.LogDebug("Sending message {MessageId} to Akka.NET DataFlow engine", message.Id);
        
        _coordinatorActor.Tell(message);
        
        // For async operation, we could implement a response mechanism
        // For now, we'll just complete the task
        await Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Akka.NET DataFlow engine with {ProcessorCount} processors", _processors.Count);
        
        // Create actor system with configuration
        var config = ConfigurationFactory.ParseString($@"
            akka {{
                actor {{
                    default-dispatcher {{
                        type = Dispatcher
                        executor = fork-join-executor
                        fork-join-executor {{
                            parallelism-min = {_configuration.MaxConcurrency}
                            parallelism-max = {_configuration.MaxConcurrency}
                            parallelism-factor = 1.0
                        }}
                    }}
                }}
                loglevel = INFO
            }}
        ");
        
        _actorSystem = ActorSystem.Create("DataFlowSystem", config);
        
        // Create coordinator actor
        _coordinatorActor = _actorSystem.ActorOf(
            CoordinatorActor.Props(_processorActors, _loggerFactory.CreateLogger<CoordinatorActor>()),
            "coordinator");
        
        // Create processor actors
        foreach (var processor in _processors.Values)
        {
            CreateProcessorActor(processor);
        }
        
        // Start all processors
        var startTasks = _processors.Values.Select(p => p.StartAsync(cancellationToken));
        await Task.WhenAll(startTasks);
        
        _logger.LogInformation("Akka.NET DataFlow engine started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Akka.NET DataFlow engine");
        
        // Stop all processors
        var stopTasks = _processors.Values.Select(p => p.StopAsync(cancellationToken));
        await Task.WhenAll(stopTasks);
        
        // Shutdown actor system
        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
            _actorSystem = null;
        }
        
        _coordinatorActor = null;
        _processorActors.Clear();
        
        _logger.LogInformation("Akka.NET DataFlow engine stopped successfully");
    }

    public IEnumerable<IDataFlowProcessor> GetProcessors()
    {
        return _processors.Values;
    }

    private void CreateProcessorActor(IDataFlowProcessor processor)
    {
        if (_actorSystem == null) return;
        
        var logger = _loggerFactory.CreateLogger<ProcessorActor>();
        var actor = _actorSystem.ActorOf(
            ProcessorActor.Props(processor, logger),
            $"processor-{processor.Id}");
        
        _processorActors.TryAdd(processor.Id, actor);
    }

    public void Dispose()
    {
        StopAsync().Wait();
    }
}