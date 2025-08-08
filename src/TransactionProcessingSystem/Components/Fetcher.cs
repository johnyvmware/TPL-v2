using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class Fetcher
{
    private readonly TransactionFetcherSettings _settings;
    private readonly ILogger<Fetcher> _logger;

    public Fetcher(
        IOptions<TransactionFetcherSettings> settings,
        ILogger<Fetcher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    // This should work per file, the fetcher
    public List<RawTransaction> FetchTransactions()
    {
        var allTransactions = new List<RawTransaction>();
        var badRecords = new List<string>();
        var inputDirectory = Path.Combine(AppContext.BaseDirectory, _settings.InputDirectory);
        var files = Directory.GetFiles(inputDirectory, "*.csv");

        var config = CsvConfiguration.FromAttributes<RawTransaction>();
        config.BadDataFound = context => badRecords.Add(context.RawRecord);

        foreach (var file in files)
        {
            using var reader = new StreamReader(file, System.Text.Encoding.GetEncoding(_settings.Encoding));
            using var csv = new CsvReader(reader, config);
            var fileTransactions = new List<RawTransaction>();

            while (csv.Read())
            {
                try
                {
                    var record = csv.GetRecord<RawTransaction>();
                    if (record != null)
                    {
                        fileTransactions.Add(record);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping null record (likely metadata) in {File}", file);
                        badRecords.Add(csv.Context?.Parser?.RawRecord ?? string.Empty);
                    }
                }
                catch (CsvHelperException csvEx)
                {
                    _logger.LogDebug("Failed to parse CSV record in {File}: {Error}", file, csvEx.Message);
                    badRecords.Add(csv.Context?.Parser?.RawRecord ?? string.Empty);
                }
            }

            allTransactions.AddRange(fileTransactions);
            _logger.LogDebug("Successfully processed {Count} transactions from {File}", fileTransactions.Count, file);
        }

        _logger.LogDebug("Processing complete. Total transactions: {Total}, Bad records: {Bad}", allTransactions.Count, badRecords.Count);

        return allTransactions;
    }
}