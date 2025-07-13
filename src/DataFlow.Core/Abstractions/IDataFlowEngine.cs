using DataFlow.Core.Models;

namespace DataFlow.Core.Abstractions;

/// <summary>
/// Interface for data flow engines
/// </summary>
public interface IDataFlowEngine
{
    /// <summary>
    /// Add a processor to the engine
    /// </summary>
    /// <param name="processor">Processor to add</param>
    void AddProcessor(IDataFlowProcessor processor);

    /// <summary>
    /// Remove a processor from the engine
    /// </summary>
    /// <param name="processorId">ID of the processor to remove</param>
    bool RemoveProcessor(string processorId);

    /// <summary>
    /// Send a message to the data flow pipeline
    /// </summary>
    /// <typeparam name="T">Type of the message payload</typeparam>
    /// <param name="message">Message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the data flow engine
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the data flow engine
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all processors in the engine
    /// </summary>
    /// <returns>Collection of processors</returns>
    IEnumerable<IDataFlowProcessor> GetProcessors();
}

/// <summary>
/// Interface for data flow engines that support configuration
/// </summary>
public interface IConfigurableDataFlowEngine : IDataFlowEngine
{
    /// <summary>
    /// Configure the engine with settings
    /// </summary>
    /// <param name="configuration">Configuration settings</param>
    void Configure(DataFlowConfiguration configuration);
}

/// <summary>
/// Configuration for data flow engines
/// </summary>
public class DataFlowConfiguration
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableBackpressure { get; set; } = true;
    public int BufferSize { get; set; } = 1000;
    public Dictionary<string, object> Properties { get; set; } = new();
}