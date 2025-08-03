using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

public class TransactionFetcherV2
{
    private readonly TransactionFetcherSettings _settings;
    private readonly ILogger<TransactionFetcherV2> _logger;

    public TransactionFetcherV2(
        IOptions<TransactionFetcherSettings> settings,
        ILogger<TransactionFetcherV2> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    // This should work per file, the fetcher
    public List<BankTransaction> FetchTransactions()
    {
        var allTransactions = new List<BankTransaction>();
        var badRecords = new List<string>();
        var files = Directory.GetFiles(_settings.InputDirectory, "*.csv");

        var config = CsvConfiguration.FromAttributes<BankTransaction>();
        config.BadDataFound = context => badRecords.Add(context.RawRecord);

        foreach (var file in files)
        {
            using var reader = new StreamReader(file, System.Text.Encoding.GetEncoding(_settings.Encoding));
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

        _logger.LogInformation("Processing complete. Total transactions: {Total}, Bad records: {Bad}", allTransactions.Count, badRecords.Count);

        return allTransactions;
    }
}