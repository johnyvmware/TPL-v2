using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class TransactionFetcher : ProcessorBase<string, IEnumerable<Transaction>>
{
    private readonly HttpClient _httpClient;
    private readonly TransactionApiSettings _settings;

    public TransactionFetcher(
        HttpClient httpClient,
        TransactionApiSettings settings,
        ILogger<TransactionFetcher> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        _httpClient = httpClient;
        _settings = settings;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    protected override async Task<IEnumerable<Transaction>> ProcessAsync(string endpoint)
    {
        _logger.LogInformation("Fetching transactions from endpoint: {Endpoint}", endpoint);

        try
        {
            var response = await FetchWithRetry(endpoint);
            var transactions = await ParseTransactions(response);

            _logger.LogInformation("Successfully fetched {Count} transactions", transactions.Count());
            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch transactions from endpoint: {Endpoint}", endpoint);
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

    private async Task<IEnumerable<Transaction>> ParseTransactions(string jsonResponse)
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

        await Task.CompletedTask; // Ensure async compliance
        return transactions;
    }
}