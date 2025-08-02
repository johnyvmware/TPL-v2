using System.Globalization;
using System.Text.Json;
using CsvHelper;
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

    protected override async Task<string> ProcessAsync(string endpoint)
    {
        var transactions = new List<BankTransaction>();
        var files = Directory.GetFiles(_settings.InputDirectory, "*.csv");

        foreach (var file in files)
        {
            using var reader = new StreamReader(file, System.Text.Encoding.GetEncoding(_settings.Encoding));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            // Skip metadata lines as specified in settings
            for (int i = 0; i < _settings.MetadataLineCount; i++)
            {
                if (reader.EndOfStream) break;
                await reader.ReadLineAsync();
            }

            var fileTransactions = csv.GetRecords<BankTransaction>().ToList();
            transactions.AddRange(fileTransactions);
        }

        return await Task.FromResult(string.Empty);
    }
}