using DataFlow.Core.Abstractions;

namespace DataFlow.Examples.Configuration;

/// <summary>
/// Configuration options for the data flow system
/// </summary>
public class DataFlowOptions
{
    public const string SectionName = "DataFlow";
    
    /// <summary>
    /// Engine type: "TPL" or "Akka"
    /// </summary>
    public string Engine { get; set; } = "TPL";
    
    /// <summary>
    /// Engine configuration
    /// </summary>
    public DataFlowConfiguration Configuration { get; set; } = new();
    
    /// <summary>
    /// Processor configurations
    /// </summary>
    public ProcessorConfiguration[] Processors { get; set; } = Array.Empty<ProcessorConfiguration>();
}

/// <summary>
/// Configuration for individual processors
/// </summary>
public class ProcessorConfiguration
{
    /// <summary>
    /// Processor ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Processor type name
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the processor is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Additional properties for the processor
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Engine type enumeration
/// </summary>
public enum EngineType
{
    TPL,
    Akka
}