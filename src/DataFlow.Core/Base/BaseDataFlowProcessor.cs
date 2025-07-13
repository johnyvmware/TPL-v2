using DataFlow.Core.Abstractions;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Base;

/// <summary>
/// Base implementation for data flow processors
/// </summary>
public abstract class BaseDataFlowProcessor : ILinkableDataFlowProcessor
{
    protected readonly ILogger Logger;
    private IDataFlowProcessor? _nextProcessor;
    private readonly object _lock = new();

    protected BaseDataFlowProcessor(string id, ILogger logger)
    {
        Id = id;
        Logger = logger;
    }

    public string Id { get; }

    public virtual async Task<DataFlowMessage?> ProcessAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Processing message {MessageId} in processor {ProcessorId}", message.Id, Id);
            
            var result = await ProcessInternalAsync(message, cancellationToken);
            
            if (result != null)
            {
                Logger.LogDebug("Message {MessageId} processed successfully in processor {ProcessorId}", message.Id, Id);
                
                // Forward to next processor if available
                var nextProcessor = GetNext();
                if (nextProcessor != null)
                {
                    await nextProcessor.ProcessAsync(result, cancellationToken);
                }
            }
            else
            {
                Logger.LogWarning("Message {MessageId} processing returned null in processor {ProcessorId}", message.Id, Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing message {MessageId} in processor {ProcessorId}", message.Id, Id);
            throw;
        }
    }

    public virtual async Task<DataFlowMessage?> ProcessAsync(DataFlowMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Processing message {MessageId} in processor {ProcessorId}", message.Id, Id);
            
            // Call the generic version using dynamic dispatch
            var result = await ProcessInternalAsync(message, cancellationToken);
            
            if (result != null)
            {
                Logger.LogDebug("Message {MessageId} processed successfully in processor {ProcessorId}", message.Id, Id);
                
                // Forward to next processor if available
                var nextProcessor = GetNext();
                if (nextProcessor != null)
                {
                    await nextProcessor.ProcessAsync(result, cancellationToken);
                }
            }
            else
            {
                Logger.LogWarning("Message {MessageId} processing returned null in processor {ProcessorId}", message.Id, Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing message {MessageId} in processor {ProcessorId}", message.Id, Id);
            throw;
        }
    }

    protected abstract Task<DataFlowMessage?> ProcessInternalAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken);

    protected virtual async Task<DataFlowMessage?> ProcessInternalAsync(DataFlowMessage message, CancellationToken cancellationToken)
    {
        // Use reflection to call the generic version
        var messageType = message.GetType();
        if (messageType.IsGenericType && messageType.GetGenericTypeDefinition() == typeof(DataFlowMessage<>))
        {
            var payloadType = messageType.GetGenericArguments()[0];
            var method = GetType().GetMethod(nameof(ProcessInternalAsync), 
                new[] { messageType, typeof(CancellationToken) });
            
            if (method != null)
            {
                var task = (Task<DataFlowMessage?>?)method.Invoke(this, new object[] { message, cancellationToken });
                return task != null ? await task : null;
            }
        }
        
        Logger.LogWarning("Unable to process message {MessageId} of type {MessageType} in processor {ProcessorId}", 
            message.Id, messageType.Name, Id);
        return null;
    }

    public virtual Task StartAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Starting processor {ProcessorId}", Id);
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Stopping processor {ProcessorId}", Id);
        return Task.CompletedTask;
    }

    public void SetNext(IDataFlowProcessor processor)
    {
        lock (_lock)
        {
            _nextProcessor = processor;
        }
    }

    public IDataFlowProcessor? GetNext()
    {
        lock (_lock)
        {
            return _nextProcessor;
        }
    }
}