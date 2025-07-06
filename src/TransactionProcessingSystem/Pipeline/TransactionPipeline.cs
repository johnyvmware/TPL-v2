using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Pipeline;

public class TransactionPipeline : IDisposable
{
    private readonly TransactionFetcher _fetcher;
    private readonly TransactionProcessor _processor;
    private readonly EmailEnricher _enricher;
    private readonly Categorizer _categorizer;
    private readonly CsvExporter _exporter;
    private readonly ILogger<TransactionPipeline> _logger;
    private readonly PipelineSettings _settings;

    public TransactionPipeline(
        TransactionFetcher fetcher,
        TransactionProcessor processor,
        EmailEnricher enricher,
        Categorizer categorizer,
        CsvExporter exporter,
        PipelineSettings settings,
        ILogger<TransactionPipeline> logger)
    {
        _fetcher = fetcher;
        _processor = processor;
        _enricher = enricher;
        _categorizer = categorizer;
        _exporter = exporter;
        _settings = settings;
        _logger = logger;

        ConnectPipeline();
    }

    private void ConnectPipeline()
    {
        // Connect fetcher to processor via transform to handle IEnumerable<Transaction>
        var fetcherToProcessor = new TransformManyBlock<IEnumerable<Transaction>, Transaction>(
            transactions => transactions,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _settings.BoundedCapacity,
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism
            });

        // Connect the pipeline stages
        _fetcher.OutputBlock.LinkTo(fetcherToProcessor, new DataflowLinkOptions { PropagateCompletion = true });
        fetcherToProcessor.LinkTo(_processor.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _processor.OutputBlock.LinkTo(_enricher.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _enricher.OutputBlock.LinkTo(_categorizer.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _categorizer.OutputBlock.LinkTo(_exporter.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

        _logger.LogInformation("Transaction pipeline connected successfully");
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
                _processor.Completion,
                _enricher.Completion,
                _categorizer.Completion,
                _exporter.Completion
            );

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(_settings.TimeoutMinutes), cancellationToken);
            var completedTask = await Task.WhenAny(completionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Pipeline processing timed out after {_settings.TimeoutMinutes} minutes");
            }

            // Ensure final flush of exporter
            await _exporter.FinalFlush();

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
        _processor?.Dispose();
        _enricher?.Dispose();
        _categorizer?.Dispose();
        _exporter?.Dispose();
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