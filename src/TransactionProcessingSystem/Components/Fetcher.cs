using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class Fetcher(
    FetcherOptions settings,
    ILogger<Fetcher> logger)
{
    // This should work per file, the fetcher
    public List<RawTransaction> Fetch()
    {
        var allTransactions = new List<RawTransaction>();
        var badRecords = new List<string>();
        var inputDirectory = Path.Combine(AppContext.BaseDirectory, settings.InputDirectory);
        var files = Directory.GetFiles(inputDirectory, "*.csv");

        var config = CsvConfiguration.FromAttributes<RawTransaction>();
        config.BadDataFound = context => badRecords.Add(context.RawRecord);

        foreach (var file in files)
        {
            using var reader = new StreamReader(file, System.Text.Encoding.GetEncoding(settings.Encoding));
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
                        badRecords.Add(csv.Context?.Parser?.RawRecord ?? string.Empty);
                    }
                }
                catch (CsvHelperException)
                {
                    badRecords.Add(csv.Context?.Parser?.RawRecord ?? string.Empty);
                }
            }

            allTransactions.AddRange(fileTransactions);
        }

        logger.LogDebug("Processing complete. Total transactions: {Total}, Bad records: {Bad}", allTransactions.Count, badRecords.Count);

        var descriptionCounts = allTransactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Description))
            .GroupBy(t => t.Description.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return allTransactions;
    }
}