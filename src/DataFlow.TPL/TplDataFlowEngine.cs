using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using DataFlow.Core.Abstractions;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.TPL;

/// <summary>
/// TPL DataFlow implementation of the data flow engine
/// </summary>
public class TplDataFlowEngine : IConfigurableDataFlowEngine
{
    private readonly ILogger<TplDataFlowEngine> _logger;
    private readonly ConcurrentDictionary<string, IDataFlowProcessor> _processors = new();
    private readonly ActionBlock<DataFlowMessage> _inputBlock;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private DataFlowConfiguration _configuration = new();

    public TplDataFlowEngine(ILogger<TplDataFlowEngine> logger)
    {
        _logger = logger;
        
        _inputBlock = new ActionBlock<DataFlowMessage>(
            async message => await ProcessMessageAsync(message, _cancellationTokenSource.Token),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _configuration.MaxConcurrency,
                BoundedCapacity = _configuration.BufferSize,
                CancellationToken = _cancellationTokenSource.Token
            });
    }

    public void Configure(DataFlowConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("TPL DataFlow engine configured with MaxConcurrency: {MaxConcurrency}, BufferSize: {BufferSize}", 
            _configuration.MaxConcurrency, _configuration.BufferSize);
    }

    public void AddProcessor(IDataFlowProcessor processor)
    {
        if (_processors.TryAdd(processor.Id, processor))
        {
            _logger.LogInformation("Added processor {ProcessorId} to TPL DataFlow engine", processor.Id);
        }
        else
        {
            _logger.LogWarning("Processor {ProcessorId} already exists in TPL DataFlow engine", processor.Id);
        }
    }

    public bool RemoveProcessor(string processorId)
    {
        if (_processors.TryRemove(processorId, out var processor))
        {
            _logger.LogInformation("Removed processor {ProcessorId} from TPL DataFlow engine", processorId);
            return true;
        }

        _logger.LogWarning("Processor {ProcessorId} not found in TPL DataFlow engine", processorId);
        return false;
    }

    public async Task SendAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message {MessageId} to TPL DataFlow engine", message.Id);
        
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token);
        
        await _inputBlock.SendAsync(message, combinedCts.Token);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting TPL DataFlow engine with {ProcessorCount} processors", _processors.Count);
        
        var startTasks = _processors.Values.Select(p => p.StartAsync(cancellationToken));
        await Task.WhenAll(startTasks);
        
        _logger.LogInformation("TPL DataFlow engine started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping TPL DataFlow engine");
        
        _inputBlock.Complete();
        await _inputBlock.Completion;
        
        var stopTasks = _processors.Values.Select(p => p.StopAsync(cancellationToken));
        await Task.WhenAll(stopTasks);
        
        _cancellationTokenSource.Cancel();
        _logger.LogInformation("TPL DataFlow engine stopped successfully");
    }

    public IEnumerable<IDataFlowProcessor> GetProcessors()
    {
        return _processors.Values;
    }

    private async Task ProcessMessageAsync(DataFlowMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing message {MessageId} in TPL DataFlow engine", message.Id);
        
        // Route message to appropriate processors
        var processingTasks = new List<Task>();
        
        foreach (var processor in _processors.Values)
        {
            var task = ProcessWithTimeoutAsync(processor, message, cancellationToken);
            processingTasks.Add(task);
        }
        
        try
        {
            await Task.WhenAll(processingTasks);
            _logger.LogDebug("Message {MessageId} processed by all processors", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} in TPL DataFlow engine", message.Id);
        }
    }

    private async Task ProcessWithTimeoutAsync(IDataFlowProcessor processor, DataFlowMessage message, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_configuration.ProcessingTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        try
        {
            await processor.ProcessAsync(message, combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Processing timeout for message {MessageId} in processor {ProcessorId}", 
                message.Id, processor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} in processor {ProcessorId}", 
                message.Id, processor.Id);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}