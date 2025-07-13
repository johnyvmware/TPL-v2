using DataFlow.Core.Base;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Examples.Processors;

/// <summary>
/// Example transform processor that transforms string messages to uppercase
/// </summary>
public class TransformProcessor : BaseTransformProcessor<string, string>
{
    public TransformProcessor(ILogger<TransformProcessor> logger) : base("transformer", logger)
    {
    }

    public override async Task<DataFlowMessage<string>?> TransformAsync(DataFlowMessage<string> input, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Transforming message {MessageId} with payload: {Payload}", input.Id, input.Payload);
        
        // Simulate transformation logic
        await Task.Delay(50, cancellationToken);
        
        var transformedPayload = input.Payload.ToUpperInvariant();
        
        Logger.LogInformation("Transformation successful for message {MessageId}: {OriginalPayload} -> {TransformedPayload}", 
            input.Id, input.Payload, transformedPayload);
        
        return new DataFlowMessage<string>
        {
            Id = input.Id,
            Payload = transformedPayload,
            Timestamp = DateTime.UtcNow,
            Properties = new Dictionary<string, object>(input.Properties)
            {
                ["TransformationTimestamp"] = DateTime.UtcNow,
                ["OriginalPayload"] = input.Payload,
                ["Transformed"] = true
            }
        };
    }
}