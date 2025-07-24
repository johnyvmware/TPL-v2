using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class TransactionFetcher : ProcessorBase<string, string>
{
    private readonly HttpClient _httpClient;
    private readonly TransactionApiSettings _settings;

    // Option 1: Traditional IOptions pattern (still works and is recommended for most cases)
    public TransactionFetcher(
        HttpClient httpClient,
        IOptions<TransactionApiSettings> settings,
        ILogger<TransactionFetcher> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    protected override async Task<string> ProcessAsync(string endpoint)
    {
        _logger.LogInformation("Fetching raw data from endpoint: {Endpoint}", endpoint);

        try
        {
            var response = await FetchWithRetry(endpoint);
            _logger.LogInformation("Successfully fetched raw response from endpoint: {Endpoint}", endpoint);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    private async Task<string> FetchWithRetry(string endpoint)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                var url = $"{_settings.BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Return the task directly instead of awaiting to avoid unnecessary state machine
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning("Attempt {Attempt} failed for endpoint {Endpoint}: {Error}",
                    attempt, endpoint, ex.Message);

                if (attempt < _settings.MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Unknown error occurred during retry");
    }
}

// Separate dataflow block for parsing transactions
public class TransactionParser : ProcessorBase<string, IEnumerable<Transaction>>
{
    public TransactionParser(
        ILogger<TransactionParser> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
    }

    protected override Task<IEnumerable<Transaction>> ProcessAsync(string jsonResponse)
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
    private IEnumerable<Transaction> ParseTransactions(string jsonResponse)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var apiResponse = JsonSerializer.Deserialize<TransactionApiResponse>(jsonResponse, options);

        if (apiResponse?.Transactions == null)
        {
            _logger.LogWarning("No transactions found in API response");
            return Enumerable.Empty<Transaction>();
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

            return new Transaction
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