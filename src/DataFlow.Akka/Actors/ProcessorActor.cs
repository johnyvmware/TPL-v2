using Akka.Actor;
using DataFlow.Core.Abstractions;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Akka.Actors;

/// <summary>
/// Actor wrapper for data flow processors
/// </summary>
public class ProcessorActor : ReceiveActor
{
    private readonly IDataFlowProcessor _processor;
    private readonly ILogger<ProcessorActor> _logger;

    public ProcessorActor(IDataFlowProcessor processor, ILogger<ProcessorActor> logger)
    {
        _processor = processor;
        _logger = logger;

        Receive<DataFlowMessage>(async message =>
        {
            try
            {
                _logger.LogDebug("Actor {ActorPath} processing message {MessageId} with processor {ProcessorId}", 
                    Self.Path, message.Id, _processor.Id);
                
                var result = await _processor.ProcessAsync(message);
                
                if (result != null)
                {
                    // Send result back to sender or forward to next processor
                    Sender.Tell(new ProcessingResult { Success = true, Message = result });
                }
                else
                {
                    Sender.Tell(new ProcessingResult { Success = false, Error = "Processing returned null" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in actor {ActorPath} processing message {MessageId} with processor {ProcessorId}", 
                    Self.Path, message.Id, _processor.Id);
                
                Sender.Tell(new ProcessingResult { Success = false, Error = ex.Message });
            }
        });

        Receive<StartProcessor>(_ =>
        {
            _logger.LogInformation("Starting processor {ProcessorId} in actor {ActorPath}", _processor.Id, Self.Path);
            _processor.StartAsync();
            Sender.Tell(new ProcessorStarted { ProcessorId = _processor.Id });
        });

        Receive<StopProcessor>(_ =>
        {
            _logger.LogInformation("Stopping processor {ProcessorId} in actor {ActorPath}", _processor.Id, Self.Path);
            _processor.StopAsync();
            Sender.Tell(new ProcessorStopped { ProcessorId = _processor.Id });
        });
    }

    public static global::Akka.Actor.Props Props(IDataFlowProcessor processor, ILogger<ProcessorActor> logger)
    {
        return global::Akka.Actor.Props.Create(() => new ProcessorActor(processor, logger));
    }
}

/// <summary>
/// Messages for processor actor communication
/// </summary>
public record ProcessingResult
{
    public bool Success { get; init; }
    public DataFlowMessage? Message { get; init; }
    public string? Error { get; init; }
}

public record StartProcessor;
public record StopProcessor;
public record ProcessorStarted { public string ProcessorId { get; init; } = string.Empty; }
public record ProcessorStopped { public string ProcessorId { get; init; } = string.Empty; }