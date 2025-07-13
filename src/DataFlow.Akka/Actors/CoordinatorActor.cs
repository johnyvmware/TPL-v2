using System.Collections.Concurrent;
using Akka.Actor;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataFlow.Akka.Actors;

/// <summary>
/// Coordinator actor for managing message flow between processors
/// </summary>
public class CoordinatorActor : ReceiveActor
{
    private readonly ConcurrentDictionary<string, IActorRef> _processorActors;
    private readonly ILogger<CoordinatorActor> _logger;

    public CoordinatorActor(ConcurrentDictionary<string, IActorRef> processorActors, ILogger<CoordinatorActor> logger)
    {
        _processorActors = processorActors;
        _logger = logger;

        Receive<DataFlowMessage>(message =>
        {
            _logger.LogDebug("Coordinator received message {MessageId}, routing to {ProcessorCount} processors", 
                message.Id, _processorActors.Count);
            
            // Route message to all processors
            foreach (var (processorId, actor) in _processorActors)
            {
                _logger.LogDebug("Routing message {MessageId} to processor {ProcessorId}", message.Id, processorId);
                actor.Tell(message);
            }
        });

        Receive<ProcessingResult>(result =>
        {
            if (result.Success)
            {
                _logger.LogDebug("Processing completed successfully for message {MessageId}", 
                    result.Message?.Id ?? "unknown");
                
                // If there's a resulting message, continue processing
                if (result.Message != null)
                {
                    // Forward to next processors (implementing pipeline logic)
                    Self.Tell(result.Message);
                }
            }
            else
            {
                _logger.LogWarning("Processing failed: {Error}", result.Error);
            }
        });

        Receive<SystemControlMessage>(controlMessage =>
        {
            _logger.LogInformation("Coordinator received control message: {Type}", controlMessage.Type);
            
            switch (controlMessage.Type)
            {
                case SystemControlType.Start:
                    foreach (var actor in _processorActors.Values)
                    {
                        actor.Tell(new StartProcessor());
                    }
                    break;
                case SystemControlType.Stop:
                    foreach (var actor in _processorActors.Values)
                    {
                        actor.Tell(new StopProcessor());
                    }
                    break;
                case SystemControlType.Shutdown:
                    Context.System.Terminate();
                    break;
            }
        });
    }

    public static global::Akka.Actor.Props Props(ConcurrentDictionary<string, IActorRef> processorActors, ILogger<CoordinatorActor> logger)
    {
        return global::Akka.Actor.Props.Create(() => new CoordinatorActor(processorActors, logger));
    }
}