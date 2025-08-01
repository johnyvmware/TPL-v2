using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class TransactionFetcher : ProcessorBase<string, string>
{
    public TransactionFetcher(
        ILogger<TransactionFetcher> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {

    }

    protected override async Task<string> ProcessAsync(string endpoint)
    {
        _logger.LogInformation("Fetching raw data from endpoint: {Endpoint}", endpoint);

        return await Task.FromResult(string.Empty);
    }
}