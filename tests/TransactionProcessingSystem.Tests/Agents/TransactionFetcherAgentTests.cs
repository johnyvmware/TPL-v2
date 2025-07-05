using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using Xunit;

namespace TransactionProcessingSystem.Tests.Agents;

public class TransactionFetcherAgentTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<TransactionFetcherAgent>> _loggerMock;
    private readonly TransactionApiSettings _settings;
    private readonly HttpListener _mockServer;
    private readonly string _baseUrl = "http://localhost:8080";

    public TransactionFetcherAgentTests()
    {
        _httpClient = new HttpClient();
        _loggerMock = new Mock<ILogger<TransactionFetcherAgent>>();
        _settings = new TransactionApiSettings
        {
            BaseUrl = _baseUrl,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        _mockServer = new HttpListener();
        _mockServer.Prefixes.Add($"{_baseUrl}/");
        _mockServer.Start();
    }

    [Fact]
    public async Task ProcessAsync_WithValidEndpoint_ReturnsTransactions()
    {
        // Arrange
        var agent = new TransactionFetcherAgent(_httpClient, _settings, _loggerMock.Object);
        var mockTransactions = new TransactionApiResponse
        {
            Transactions = new[]
            {
                new RawTransaction
                {
                    Id = "1",
                    Date = "2024-01-01",
                    Amount = "100.50",
                    Description = "Test Transaction"
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(mockTransactions);
        StartMockServer(responseJson);

        // Act
        var result = await agent.ProcessAsync("api/transactions");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var transaction = result.First();
        transaction.Id.Should().Be("1");
        transaction.Amount.Should().Be(100.50m);
        transaction.Description.Should().Be("Test Transaction");
        transaction.Status.Should().Be(ProcessingStatus.Fetched);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidJsonResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var agent = new TransactionFetcherAgent(_httpClient, _settings, _loggerMock.Object);
        StartMockServer("invalid json");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => agent.ProcessAsync("api/transactions"));
    }

    [Fact]
    public async Task ProcessAsync_WithServerError_RetriesAndThrows()
    {
        // Arrange
        var agent = new TransactionFetcherAgent(_httpClient, _settings, _loggerMock.Object);
        StartMockServer("", HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => agent.ProcessAsync("api/transactions"));
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var agent = new TransactionFetcherAgent(_httpClient, _settings, _loggerMock.Object);
        var emptyResponse = new TransactionApiResponse { Transactions = Array.Empty<RawTransaction>() };
        var responseJson = JsonSerializer.Serialize(emptyResponse);
        StartMockServer(responseJson);

        // Act
        var result = await agent.ProcessAsync("api/transactions");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidDateFormat_UsesMinValue()
    {
        // Arrange
        var agent = new TransactionFetcherAgent(_httpClient, _settings, _loggerMock.Object);
        var mockTransactions = new TransactionApiResponse
        {
            Transactions = new[]
            {
                new RawTransaction
                {
                    Id = "1",
                    Date = "invalid-date",
                    Amount = "100.50",
                    Description = "Test Transaction"
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(mockTransactions);
        StartMockServer(responseJson);

        // Act
        var result = await agent.ProcessAsync("api/transactions");

        // Assert
        result.Should().HaveCount(1);
        var transaction = result.First();
        transaction.Date.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidAmountFormat_UsesZero()
    {
        // Arrange
        var agent = new TransactionFetcherAgent(_httpClient, _settings, _loggerMock.Object);
        var mockTransactions = new TransactionApiResponse
        {
            Transactions = new[]
            {
                new RawTransaction
                {
                    Id = "1",
                    Date = "2024-01-01",
                    Amount = "invalid-amount",
                    Description = "Test Transaction"
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(mockTransactions);
        StartMockServer(responseJson);

        // Act
        var result = await agent.ProcessAsync("api/transactions");

        // Assert
        result.Should().HaveCount(1);
        var transaction = result.First();
        transaction.Amount.Should().Be(0m);
    }

    private void StartMockServer(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Task.Run(async () =>
        {
            var context = await _mockServer.GetContextAsync();
            var httpResponse = context.Response;
            httpResponse.StatusCode = (int)statusCode;
            httpResponse.ContentType = "application/json";
            
            var buffer = System.Text.Encoding.UTF8.GetBytes(response);
            httpResponse.ContentLength64 = buffer.Length;
            await httpResponse.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            httpResponse.OutputStream.Close();
        });
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockServer?.Stop();
        _mockServer?.Close();
    }
}