using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class TransactionFetcher : ProcessorBase<string, string>
{
    private readonly TransactionFetcherSettings _settings;

    public TransactionFetcher(
        IOptions<TransactionFetcherSettings> settings,
        ILogger<TransactionFetcher> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        _settings = settings.Value;
    }

    public async Task FetchTransactionsAsync()
    {
        var transactions = await ProcessAsync(string.Empty);
    }

   protected override async Task<string> ProcessAsync(string endpoint)
    {
        var allTransactions = new List<BankTransaction>();
        var badRecords = new List<string>();
        var files = Directory.GetFiles(_settings.InputDirectory, "*.csv");

        var config = CsvConfiguration.FromAttributes<BankTransaction>();
        config.BadDataFound = context => badRecords.Add(context.RawRecord);

        foreach (var file in files)
        {
            using var reader = new StreamReader(file, System.Text.Encoding.GetEncoding(_settings.Encoding));
            
            // Skip header metadata lines as specified in settings
            for (int i = 0; i < _settings.MetadataLineCount; i++)
            {
                if (reader.EndOfStream) break;
                await reader.ReadLineAsync();
            }

            using var csv = new CsvReader(reader, config);
            var fileTransactions = new List<BankTransaction>();

            while (csv.Read())
            {
                try
                {
                    var record = csv.GetRecord<BankTransaction>();
                    if (record != null)
                    {
                        fileTransactions.Add(record);
                    }
                    else
                    {
                        // Record is null - treat as metadata/bad data
                        _logger.LogDebug("Skipping null record (likely metadata) in {File}", file);
                        badRecords.Add(csv.Context?.Parser?.RawRecord ?? string.Empty);
                    }
                }
                catch (CsvHelperException csvEx)
                {
                    _logger.LogWarning("Failed to parse CSV record in {File}: {Error}", file, csvEx.Message);
                    badRecords.Add(csv.Context?.Parser?.RawRecord ?? string.Empty);
                }
            }

            allTransactions.AddRange(fileTransactions);
            _logger.LogInformation("Successfully processed {Count} transactions from {File}", fileTransactions.Count, file);
        }

        _logger.LogInformation("Processing complete. Total transactions: {Total}, Bad records: {Bad}", 
            allTransactions.Count, badRecords.Count);

        return await Task.FromResult(string.Empty);
    }
}