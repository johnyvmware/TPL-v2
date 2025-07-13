using DataFlow.Core.Base;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Examples.Processors;

/// <summary>
/// Example logging processor that logs messages and passes them through
/// </summary>
public class LoggingProcessor : BaseDataFlowProcessor
{
    public LoggingProcessor(ILogger<LoggingProcessor> logger) : base("logger", logger)
    {
    }

    protected override async Task<DataFlowMessage?> ProcessInternalAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Processing message {MessageId} of type {MessageType}", message.Id, typeof(T).Name);
        Logger.LogInformation("Message payload: {Payload}", message.Payload);
        Logger.LogInformation("Message properties: {Properties}", 
            string.Join(", ", message.Properties.Select(p => $"{p.Key}={p.Value}")));
        
        // Simulate logging delay
        await Task.Delay(10, cancellationToken);
        
        // Add logging information to properties
        var updatedProperties = new Dictionary<string, object>(message.Properties)
        {
            ["LoggedAt"] = DateTime.UtcNow,
            ["LoggedBy"] = Id
        };
        
        // Create a new message with updated properties
        var loggedMessage = new DataFlowMessage<T>
        {
            Id = message.Id,
            Payload = message.Payload,
            Timestamp = message.Timestamp,
            Properties = updatedProperties
        };
        
        Logger.LogInformation("Logging completed for message {MessageId}", message.Id);
        
        return loggedMessage;
    }
}