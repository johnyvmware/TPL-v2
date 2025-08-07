using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Services;

public class MockTransactionApiService : BackgroundService
{
    private readonly ILogger<MockTransactionApiService> _logger;
    private readonly HttpListener _listener;
    private readonly string _baseUrl;

    public MockTransactionApiService(ILogger<MockTransactionApiService> logger, string baseUrl = "http://localhost:5000")
    {
        _logger = logger;
        _baseUrl = baseUrl;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"{_baseUrl}/");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _listener.Start();
            _logger.LogInformation("Mock Transaction API started at {BaseUrl}", _baseUrl);

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(async () => await HandleRequest(context), stoppingToken);
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 5) // Access denied
        {
            _logger.LogWarning("Access denied when starting HTTP listener. Run as administrator or use a different port.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mock transaction API service");
        }
        finally
        {
            _listener?.Stop();
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            _logger.LogDebug("Received request: {Method} {Url}", request.HttpMethod, request.Url);

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/api/transactions")
            {
                await HandleTransactionsRequest(response);
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponse(response, "Not Found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request");
            try
            {
                context.Response.StatusCode = 500;
                await WriteResponse(context.Response, "Internal Server Error");
            }
            catch
            {
                // Ignore errors when writing error response
            }
        }
    }

    private async Task HandleTransactionsRequest(HttpListenerResponse response)
    {
        var mockTransactions = GenerateMockTransactions();
        var apiResponse = new TransactionApiResponse
        {
            Transactions = mockTransactions
        };

        var json = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        response.ContentType = "application/json";
        response.StatusCode = 200;
        await WriteResponse(response, json);

        _logger.LogInformation("Served {Count} mock transactions", mockTransactions.Count());
    }

    private async Task WriteResponse(HttpListenerResponse response, string content)
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private IEnumerable<Models.RawTransactionOld> GenerateMockTransactions()
    {
        var random = new Random();
        var merchants = new[]
        {
            "McDonald's Restaurant #1234",
            "Shell Gas Station",
            "Amazon Purchase - Multiple Items",
            "Walmart Supercenter",
            "Starbucks Coffee",
            "Netflix Monthly Subscription",
            "Electric Company Bill Payment",
            "Target Store Purchase",
            "Uber Ride Service",
            "CVS Pharmacy",
            "Best Buy Electronics",
            "Whole Foods Market",
            "Chase Bank ATM Withdrawal",
            "Spotify Premium Subscription",
            "Home Depot Hardware Store"
        };

        var transactions = new List<Models.RawTransactionOld>();
        var baseDate = DateTime.Today.AddDays(-30);

        for (int i = 0; i < 20; i++)
        {
            var merchant = merchants[random.Next(merchants.Length)];
            var amount = (decimal)(random.NextDouble() * 200 + 5); // $5-$205
            var date = baseDate.AddDays(random.Next(60)); // Within last 30 days to next 30 days

            transactions.Add(new Models.RawTransactionOld
            {
                Id = Guid.NewGuid().ToString(),
                Date = date.ToString("yyyy-MM-dd"),
                Amount = amount.ToString("F2"),
                Description = merchant
            });
        }

        return transactions.OrderBy(t => DateTime.Parse(t.Date));
    }

    public override void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
        base.Dispose();
    }
}