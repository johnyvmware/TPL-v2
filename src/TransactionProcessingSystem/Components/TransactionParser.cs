using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class TransactionParser : ProcessorBase<string, IEnumerable<TransactionOld>>
{
    public TransactionParser(
        ILogger<TransactionParser> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
    }

    protected override Task<IEnumerable<TransactionOld>> ProcessAsync(string jsonResponse)
    {
        _logger.LogDebug("Parsing transactions from JSON response");

        try
        {
            var transactions = ParseTransactions(jsonResponse);
            _logger.LogInformation("Successfully parsed {Count} transactions", transactions.Count());
            return Task.FromResult(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse transactions from JSON response");
            throw;
        }
    }

    // Parse transaction in separate block and made synchronous since there's no async code
    private IEnumerable<TransactionOld> ParseTransactions(string jsonResponse)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var apiResponse = JsonSerializer.Deserialize<TransactionApiResponse>(jsonResponse, options);

        if (apiResponse?.Transactions == null)
        {
            _logger.LogWarning("No transactions found in API response");
            return Enumerable.Empty<TransactionOld>();
        }

        var transactions = apiResponse.Transactions.Select(raw =>
        {
            if (!DateTime.TryParse(raw.Date, out var date))
            {
                _logger.LogWarning("Invalid date format for transaction {Id}: {Date}", raw.Id, raw.Date);
                date = DateTime.MinValue;
            }

            if (!decimal.TryParse(raw.Amount, out var amount))
            {
                _logger.LogWarning("Invalid amount format for transaction {Id}: {Amount}", raw.Id, raw.Amount);
                amount = 0m;
            }

            return new TransactionOld
            {
                Id = raw.Id,
                Date = date,
                Amount = amount,
                Description = raw.Description,
                Status = ProcessingStatus.Fetched
            };
        }).ToList();

        return transactions;
    }
}