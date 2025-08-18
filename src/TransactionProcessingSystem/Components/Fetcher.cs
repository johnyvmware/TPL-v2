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
    public List<Transaction> Fetch()
    {
        var allTransactions = new List<Transaction>();
        var badRecords = new List<string>();
        var inputDirectory = Path.Combine(AppContext.BaseDirectory, settings.InputDirectory);
        var files = Directory.GetFiles(inputDirectory, "*.csv");

        var config = CsvConfiguration.FromAttributes<Transaction>();
        config.BadDataFound = context => badRecords.Add(context.RawRecord);

        foreach (var file in files)
        {
            using var reader = new StreamReader(file, System.Text.Encoding.GetEncoding(settings.Encoding));
            using var csv = new CsvReader(reader, config);
            var fileTransactions = new List<Transaction>();

            while (csv.Read())
            {
                try
                {
                    var record = csv.GetRecord<Transaction>();
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

        return allTransactions;
    }
}