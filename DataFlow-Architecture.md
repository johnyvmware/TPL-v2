# Modular Data Flow Architecture

This project demonstrates a modular data flow system using an agent programming model that supports both TPL DataFlow and Akka.NET frameworks. The architecture is designed to be configurable, allowing you to switch between different implementations at runtime.

## Architecture Overview

### Core Components

1. **DataFlow.Core** - Contains the core abstractions and interfaces
2. **DataFlow.TPL** - TPL DataFlow implementation
3. **DataFlow.Akka** - Akka.NET implementation
4. **DataFlow.Examples** - Example application demonstrating usage

### Key Features

- **Modular Design**: Clean separation between core abstractions and implementations
- **Configurable Engine**: Switch between TPL and Akka.NET engines via configuration
- **Type-Safe Messages**: Strong typing for data flow messages
- **Extensible Processors**: Easy to create custom processors
- **Async/Await Support**: Full async support throughout the pipeline
- **Logging Integration**: Comprehensive logging support

## Core Abstractions

### IDataFlowEngine

The main engine interface that manages processors and message routing:

```csharp
public interface IDataFlowEngine
{
    void AddProcessor(IDataFlowProcessor processor);
    bool RemoveProcessor(string processorId);
    Task SendAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    IEnumerable<IDataFlowProcessor> GetProcessors();
}
```

### IDataFlowProcessor

Base processor interface for handling messages:

```csharp
public interface IDataFlowProcessor
{
    string Id { get; }
    Task<DataFlowMessage?> ProcessAsync<T>(DataFlowMessage<T> message, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### DataFlowMessage

Type-safe message wrapper:

```csharp
public record DataFlowMessage<T> : DataFlowMessage
{
    public T Payload { get; init; } = default!;
}
```

## Implementation Details

### TPL DataFlow Implementation

Uses `ActionBlock<T>` for message processing with configurable parallelism:

- **Advantages**: Lightweight, built-in backpressure, excellent performance
- **Use Cases**: High-throughput scenarios, CPU-intensive processing

### Akka.NET Implementation

Uses actor model with coordinator and processor actors:

- **Advantages**: Distributed processing, fault tolerance, location transparency
- **Use Cases**: Distributed systems, complex state management, fault-tolerant processing

## Configuration

### Engine Selection

Choose between TPL and Akka.NET engines in `appsettings.json`:

```json
{
  "DataFlow": {
    "Engine": "TPL",
    "Configuration": {
      "MaxConcurrency": 4,
      "ProcessingTimeout": "00:00:30",
      "EnableBackpressure": true,
      "BufferSize": 1000
    }
  }
}
```

### Processor Configuration

Configure which processors to load:

```json
{
  "DataFlow": {
    "Processors": [
      {
        "Id": "validator",
        "Type": "ValidationProcessor",
        "Enabled": true
      },
      {
        "Id": "transformer",
        "Type": "TransformProcessor",
        "Enabled": true
      }
    ]
  }
}
```

## Usage Examples

### Running with TPL DataFlow

```bash
# Use TPL engine (default)
dotnet run --project src/DataFlow.Examples
```

### Running with Akka.NET

```bash
# Update appsettings.json to use "Akka" engine
dotnet run --project src/DataFlow.Examples
```

### Custom Processor Example

```csharp
public class CustomProcessor : BaseTransformProcessor<string, string>
{
    public CustomProcessor(ILogger<CustomProcessor> logger) : base("custom", logger)
    {
    }

    public override async Task<DataFlowMessage<string>?> TransformAsync(
        DataFlowMessage<string> input, 
        CancellationToken cancellationToken = default)
    {
        // Custom processing logic
        var result = await ProcessAsync(input.Payload, cancellationToken);
        
        return new DataFlowMessage<string>
        {
            Payload = result,
            Properties = new Dictionary<string, object>(input.Properties)
            {
                ["ProcessedBy"] = Id,
                ["ProcessedAt"] = DateTime.UtcNow
            }
        };
    }
}
```

## Benefits of This Architecture

1. **Modularity**: Each implementation is in its own assembly
2. **Testability**: Core abstractions make unit testing easier
3. **Flexibility**: Easy to switch between implementations
4. **Scalability**: Both TPL and Akka.NET support high concurrency
5. **Maintainability**: Clear separation of concerns

## Performance Considerations

### TPL DataFlow
- Best for CPU-intensive, high-throughput scenarios
- Lower memory overhead
- Excellent for single-machine processing

### Akka.NET
- Best for distributed, fault-tolerant scenarios
- Higher memory overhead due to actor model
- Excellent for complex state management and distributed processing

## Future Enhancements

1. **Message Persistence**: Add support for durable message queues
2. **Metrics Collection**: Add performance monitoring
3. **Circuit Breaker**: Add fault tolerance patterns
4. **Dynamic Configuration**: Hot-reload configuration changes
5. **Additional Engines**: Support for other frameworks (e.g., Orleans, Service Fabric)

## Getting Started

1. Clone the repository
2. Build the solution: `dotnet build`
3. Run the example: `dotnet run --project src/DataFlow.Examples`
4. Modify `appsettings.json` to switch between engines
5. Add custom processors as needed

This architecture provides a solid foundation for building scalable, maintainable data flow systems with the flexibility to choose the most appropriate implementation for your specific use case.