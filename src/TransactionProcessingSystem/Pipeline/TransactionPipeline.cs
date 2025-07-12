using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Pipeline;

public class TransactionPipeline : IDisposable
{
    private readonly TransactionFetcher _fetcher;
    private readonly TransactionParser _parser;
    private readonly TransactionProcessor _processor;
    private readonly EmailEnricher _enricher;
    private readonly Categorizer _categorizer;
    private readonly Neo4jExporter _neo4jExporter;
    private readonly ILogger<TransactionPipeline> _logger;
    private readonly PipelineSettings _settings;

    public TransactionPipeline(
        TransactionFetcher fetcher,
        TransactionParser parser,
        TransactionProcessor processor,
        EmailEnricher enricher,
        Categorizer categorizer,
        Neo4jExporter neo4jExporter,
        PipelineSettings settings,
        ILogger<TransactionPipeline> logger)
    {
        _fetcher = fetcher;
        _parser = parser;
        _processor = processor;
        _enricher = enricher;
        _categorizer = categorizer;
        _neo4jExporter = neo4jExporter;
        _settings = settings;
        _logger = logger;

        ConnectPipeline();
    }

    private void ConnectPipeline()
    {
        // Connect parser to processor via transform to handle IEnumerable<Transaction>
        var parserToProcessor = new TransformManyBlock<IEnumerable<Transaction>, Transaction>(
            transactions => transactions,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _settings.BoundedCapacity,
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism
            });

        // Connect the pipeline stages: fetcher -> parser -> transformMany -> processor -> enricher -> categorizer -> exporter
        _fetcher.OutputBlock.LinkTo(_parser.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _parser.OutputBlock.LinkTo(parserToProcessor, new DataflowLinkOptions { PropagateCompletion = true });
        parserToProcessor.LinkTo(_processor.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _processor.OutputBlock.LinkTo(_enricher.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _enricher.OutputBlock.LinkTo(_categorizer.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _categorizer.OutputBlock.LinkTo(_neo4jExporter.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

        _logger.LogInformation("Transaction pipeline connected successfully with separate parser block");
    }

    public async Task<PipelineResult> ProcessTransactionsAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting transaction processing pipeline for endpoint: {Endpoint}", endpoint);

        var startTime = DateTime.UtcNow;
        var result = new PipelineResult
        {
            StartTime = startTime,
            Endpoint = endpoint
        };

        try
        {
            // Start processing by posting to the fetcher
            var posted = await _fetcher.InputBlock.SendAsync(endpoint, cancellationToken);
            if (!posted)
            {
                throw new InvalidOperationException("Failed to post endpoint to fetcher");
            }

            // Signal completion to start the completion propagation
            _fetcher.Complete();

            // Wait for pipeline completion with timeout
            var completionTask = Task.WhenAll(
                _fetcher.Completion,
                _parser.Completion,
                _processor.Completion,
                _enricher.Completion,
                _categorizer.Completion,
                _neo4jExporter.Completion
            );

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(_settings.TimeoutMinutes), cancellationToken);
            var completedTask = await Task.WhenAny(completionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Pipeline processing timed out after {_settings.TimeoutMinutes} minutes");
            }

            // CSV exporter removed - using Neo4j export instead

            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.Success = true;

            _logger.LogInformation("Transaction processing pipeline completed successfully in {Duration}",
                result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.Success = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Transaction processing pipeline failed after {Duration}",
                result.Duration);

            throw;
        }
    }

    public void Dispose()
    {
        _fetcher?.Dispose();
        _parser?.Dispose();
        _processor?.Dispose();
        _enricher?.Dispose();
        _categorizer?.Dispose();
        _neo4jExporter?.Dispose();
    }
}

public record PipelineResult
{
    public required DateTime StartTime { get; init; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public required string Endpoint { get; init; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TransactionsProcessed { get; set; }
}