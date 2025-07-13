using DataFlow.Core.Base;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Examples.Processors;

/// <summary>
/// Example validation processor that validates string messages
/// </summary>
public class ValidationProcessor : BaseTransformProcessor<string, string>
{
    public ValidationProcessor(ILogger<ValidationProcessor> logger) : base("validator", logger)
    {
    }

    public override async Task<DataFlowMessage<string>?> TransformAsync(DataFlowMessage<string> input, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Validating message {MessageId} with payload: {Payload}", input.Id, input.Payload);
        
        // Simulate validation logic
        await Task.Delay(100, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(input.Payload))
        {
            Logger.LogWarning("Validation failed for message {MessageId}: Empty payload", input.Id);
            return null;
        }
        
        if (input.Payload.Length > 1000)
        {
            Logger.LogWarning("Validation failed for message {MessageId}: Payload too long", input.Id);
            return null;
        }
        
        Logger.LogInformation("Validation successful for message {MessageId}", input.Id);
        
        return new DataFlowMessage<string>
        {
            Id = input.Id,
            Payload = input.Payload,
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>(input.Properties)
            {
                ["ValidationTimestamp"] = DateTime.UtcNow,
                ["Validated"] = true
            }
        };
    }
}