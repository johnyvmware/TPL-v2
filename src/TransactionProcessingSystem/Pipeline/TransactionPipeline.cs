using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Pipeline;

public class TransactionPipeline : IDisposable
{
    private readonly TransactionFetcherAgent _fetcherAgent;
    private readonly TransactionProcessorAgent _processorAgent;
    private readonly EmailEnricherAgent _enricherAgent;
    private readonly CategorizerAgent _categorizerAgent;
    private readonly CsvExporterAgent _exporterAgent;
    private readonly ILogger<TransactionPipeline> _logger;
    private readonly PipelineSettings _settings;

    public TransactionPipeline(
        TransactionFetcherAgent fetcherAgent,
        TransactionProcessorAgent processorAgent,
        EmailEnricherAgent enricherAgent,
        CategorizerAgent categorizerAgent,
        CsvExporterAgent exporterAgent,
        PipelineSettings settings,
        ILogger<TransactionPipeline> logger)
    {
        _fetcherAgent = fetcherAgent;
        _processorAgent = processorAgent;
        _enricherAgent = enricherAgent;
        _categorizerAgent = categorizerAgent;
        _exporterAgent = exporterAgent;
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
        _fetcherAgent.OutputBlock.LinkTo(fetcherToProcessor, new DataflowLinkOptions { PropagateCompletion = true });
        fetcherToProcessor.LinkTo(_processorAgent.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _processorAgent.OutputBlock.LinkTo(_enricherAgent.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _enricherAgent.OutputBlock.LinkTo(_categorizerAgent.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _categorizerAgent.OutputBlock.LinkTo(_exporterAgent.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });

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
            var posted = await _fetcherAgent.InputBlock.SendAsync(endpoint, cancellationToken);
            if (!posted)
            {
                throw new InvalidOperationException("Failed to post endpoint to fetcher agent");
            }

            // Signal completion to start the completion propagation
            _fetcherAgent.Complete();

            // Wait for pipeline completion with timeout
            var completionTask = Task.WhenAll(
                _fetcherAgent.Completion,
                _processorAgent.Completion,
                _enricherAgent.Completion,
                _categorizerAgent.Completion,
                _exporterAgent.Completion
            );

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(_settings.TimeoutMinutes), cancellationToken);
            var completedTask = await Task.WhenAny(completionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Pipeline processing timed out after {_settings.TimeoutMinutes} minutes");
            }

            // Ensure final flush of exporter
            await _exporterAgent.FinalFlush();

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
        _fetcherAgent?.Dispose();
        _processorAgent?.Dispose();
        _enricherAgent?.Dispose();
        _categorizerAgent?.Dispose();
        _exporterAgent?.Dispose();
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