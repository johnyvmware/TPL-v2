namespace DataFlow.Core.Models;

/// <summary>
/// Base class for all data flow messages
/// </summary>
public abstract record DataFlowMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Generic data flow message with payload
/// </summary>
/// <typeparam name="T">Type of the payload</typeparam>
public record DataFlowMessage<T> : DataFlowMessage
{
    public T Payload { get; init; } = default!;
}

/// <summary>
/// Message for processing completion
/// </summary>
public record ProcessingCompletedMessage : DataFlowMessage
{
    public string ProcessorId { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Message for system control
/// </summary>
public record SystemControlMessage : DataFlowMessage
{
    public SystemControlType Type { get; init; }
    public string? TargetProcessor { get; init; }
}

public enum SystemControlType
{
    Start,
    Stop,
    Pause,
    Resume,
    Shutdown
}