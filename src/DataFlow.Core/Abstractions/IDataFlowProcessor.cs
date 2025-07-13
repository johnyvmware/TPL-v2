using DataFlow.Core.Models;

namespace DataFlow.Core.Abstractions;

/// <summary>
/// Interface for data flow processors
/// </summary>
public interface IDataFlowProcessor
{
    /// <summary>
    /// Unique identifier for the processor
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Process a message asynchronously
    /// </summary>
    /// <typeparam name="T">Type of the message payload</typeparam>
    /// <param name="message">Message to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed message or null if processing failed</returns>
    Task<DataFlowMessage?> ProcessAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a message asynchronously (non-generic version for chaining)
    /// </summary>
    /// <param name="message">Message to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processed message or null if processing failed</returns>
    Task<DataFlowMessage?> ProcessAsync(DataFlowMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the processor
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the processor
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for processors that can be linked together
/// </summary>
public interface ILinkableDataFlowProcessor : IDataFlowProcessor
{
    /// <summary>
    /// Set the next processor in the chain
    /// </summary>
    /// <param name="processor">Next processor</param>
    void SetNext(IDataFlowProcessor processor);

    /// <summary>
    /// Get the next processor in the chain
    /// </summary>
    /// <returns>Next processor or null if this is the last processor</returns>
    IDataFlowProcessor? GetNext();
}

/// <summary>
/// Interface for processors that can transform message types
/// </summary>
/// <typeparam name="TInput">Input message type</typeparam>
/// <typeparam name="TOutput">Output message type</typeparam>
public interface ITransformProcessor<TInput, TOutput> : IDataFlowProcessor
{
    /// <summary>
    /// Transform input message to output message
    /// </summary>
    /// <param name="input">Input message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transformed output message</returns>
    Task<DataFlowMessage<TOutput>?> TransformAsync(DataFlowMessage<TInput> input, CancellationToken cancellationToken = default);
}