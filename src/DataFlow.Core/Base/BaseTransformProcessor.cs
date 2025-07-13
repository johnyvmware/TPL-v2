using DataFlow.Core.Abstractions;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Base;

/// <summary>
/// Base implementation for transform processors
/// </summary>
/// <typeparam name="TInput">Input message type</typeparam>
/// <typeparam name="TOutput">Output message type</typeparam>
public abstract class BaseTransformProcessor<TInput, TOutput> : BaseDataFlowProcessor, ITransformProcessor<TInput, TOutput>
{
    protected BaseTransformProcessor(string id, ILogger logger) : base(id, logger)
    {
    }

    protected override async Task<DataFlowMessage?> ProcessInternalAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken)
    {
        if (message is DataFlowMessage<TInput> inputMessage)
        {
            return await TransformAsync(inputMessage, cancellationToken);
        }

        Logger.LogWarning("Message {MessageId} type {MessageType} is not compatible with processor {ProcessorId} input type {InputType}", 
            message.Id, typeof(T).Name, Id, typeof(TInput).Name);
        
        return null;
    }

    public abstract Task<DataFlowMessage<TOutput>?> TransformAsync(DataFlowMessage<TInput> input, CancellationToken cancellationToken = default);
}