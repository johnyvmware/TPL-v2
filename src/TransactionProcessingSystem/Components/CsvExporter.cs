using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Threading.Tasks.Dataflow;
using CsvHelper;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class CsvExporter : IDisposable
{
    private readonly ActionBlock<Transaction> _block;
    private readonly ConcurrentQueue<Transaction> _buffer;
    private readonly ExportSettings _settings;
    private readonly ILogger<CsvExporter> _logger;
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _exportSemaphore;
    private readonly string _outputDirectory;
    private int _totalExported;

    public CsvExporter(
        ExportSettings settings,
        ILogger<CsvExporter> logger,
        int boundedCapacity = 100)
    {
        _settings = settings;
        _logger = logger;
        _buffer = new ConcurrentQueue<Transaction>();
        _exportSemaphore = new SemaphoreSlim(1, 1);
        _totalExported = 0;

        // Ensure output directory exists
        _outputDirectory = Path.GetFullPath(_settings.OutputDirectory);
        Directory.CreateDirectory(_outputDirectory);

        var options = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = boundedCapacity,
            MaxDegreeOfParallelism = 1 // Single-threaded for consistent ordering
        };

        _block = new ActionBlock<Transaction>(ProcessTransaction, options);

        // Set up periodic flush every 30 seconds
        _flushTimer = new Timer(async _ => await FlushBuffer(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public ITargetBlock<Transaction> InputBlock => _block;
    public Task Completion => _block.Completion;

    public void Complete()
    {
        _block.Complete();
    }

    private async Task ProcessTransaction(Transaction transaction)
    {
        _logger.LogDebug("Buffering transaction {Id} for export", transaction.Id);

        _buffer.Enqueue(transaction);

        // Check if we should flush the buffer
        if (_buffer.Count >= _settings.BufferSize)
        {
            await FlushBuffer();
        }
    }

    private async Task FlushBuffer()
    {
        await _exportSemaphore.WaitAsync();
        try
        {
            if (_buffer.IsEmpty)
                return;

            var transactions = new List<Transaction>();
            while (_buffer.TryDequeue(out var transaction))
            {
                transactions.Add(transaction);
            }

            if (transactions.Any())
            {
                await ExportTransactions(transactions);
                _totalExported += transactions.Count;
                _logger.LogInformation("Exported {Count} transactions (Total: {Total})",
                    transactions.Count, _totalExported);
            }
        }
        finally
        {
            _exportSemaphore.Release();
        }
    }

    private async Task ExportTransactions(List<Transaction> transactions)
    {
        var fileName = string.Format(_settings.FileNameFormat, DateTime.Now);
        var filePath = Path.Combine(_outputDirectory, fileName);

        var csvRecords = transactions.Select(t => new CsvTransactionRecord
        {
            Date = t.Date.ToString("yyyy-MM-dd"),
            Amount = t.Amount.ToString("F2"),
            Description = t.CleanDescription ?? t.Description,
            Category = t.Category ?? "Uncategorized",
            EmailSubject = t.EmailSubject ?? "",
            EmailSnippet = t.EmailSnippet ?? ""
        }).ToList();

        try
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Write header
            csv.WriteField("Date");
            csv.WriteField("Amount");
            csv.WriteField("Description");
            csv.WriteField("Category");
            csv.WriteField("Email Subject");
            csv.WriteField("Email Snippet");
            csv.NextRecord();

            // Write records
            foreach (var record in csvRecords)
            {
                csv.WriteField(record.Date);
                csv.WriteField(record.Amount);
                csv.WriteField(record.Description);
                csv.WriteField(record.Category);
                csv.WriteField(record.EmailSubject);
                csv.WriteField(record.EmailSnippet);
                csv.NextRecord();
            }

            var csvContent = writer.ToString();
            await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);

            _logger.LogInformation("Successfully exported {Count} transactions to {FilePath}",
                transactions.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export transactions to {FilePath}", filePath);
            throw;
        }
    }

    public async Task FinalFlush()
    {
        _logger.LogInformation("Performing final flush of remaining transactions");
        await FlushBuffer();
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _exportSemaphore?.Dispose();

        // Ensure final flush on disposal
        try
        {
            FlushBuffer().Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final flush");
        }
    }
}

public record CsvTransactionRecord
{
    public required string Date { get; init; }
    public required string Amount { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string EmailSubject { get; init; }
    public required string EmailSnippet { get; init; }
}