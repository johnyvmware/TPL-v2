using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Pipeline;
using Xunit;
using System.Net;
using System.Text.Json;

namespace TransactionProcessingSystem.Tests.Pipeline;

public class TransactionPipelineTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly HttpListener _mockServer;
    private readonly string _baseUrl = "http://localhost:8081";
    private readonly Mock<ILogger<TransactionPipeline>> _pipelineLoggerMock;
    private readonly TransactionApiSettings _apiSettings;
    private readonly ExportSettings _exportSettings;
    private readonly PipelineSettings _pipelineSettings;
    private readonly OpenAISettings _openAISettings;
    private readonly MicrosoftGraphSettings _graphSettings;

    public TransactionPipelineTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PipelineTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        _pipelineLoggerMock = new Mock<ILogger<TransactionPipeline>>();
        
        _apiSettings = new TransactionApiSettings
        {
            BaseUrl = _baseUrl,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        _exportSettings = new ExportSettings
        {
            OutputDirectory = _tempDirectory,
            FileNameFormat = "pipeline_test_{0:yyyyMMdd_HHmmss}.csv",
            BufferSize = 5
        };

        _pipelineSettings = new PipelineSettings
        {
            BoundedCapacity = 10,
            MaxDegreeOfParallelism = 2,
            TimeoutMinutes = 1
        };

        _openAISettings = new OpenAISettings
        {
            ApiKey = "test-key",
            Model = "gpt-3.5-turbo"
        };

        _graphSettings = new MicrosoftGraphSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TenantId = "test-tenant-id"
        };

        _mockServer = new HttpListener();
        _mockServer.Prefixes.Add($"{_baseUrl}/");
        _mockServer.Start();
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithValidData_ProcessesFullPipeline()
    {
        // Arrange
        var mockTransactions = new TransactionApiResponse
        {
            Transactions = new[]
            {
                new RawTransaction
                {
                    Id = "1",
                    Date = "2024-01-01",
                    Amount = "25.50",
                    Description = "McDonald's Restaurant"
                },
                new RawTransaction
                {
                    Id = "2",
                    Date = "2024-01-02",
                    Amount = "45.75",
                    Description = "Shell Gas Station"
                },
                new RawTransaction
                {
                    Id = "3",
                    Date = "2024-01-03",
                    Amount = "125.99",
                    Description = "Amazon Purchase"
                }
            }
        };

        StartMockServer(JsonSerializer.Serialize(mockTransactions));

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);

        // Verify CSV file was created
        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        csvFiles.Should().HaveCount(1);

        var csvContent = await File.ReadAllTextAsync(csvFiles[0]);
        csvContent.Should().Contain("Date,Amount,Description,Category");
        csvContent.Should().Contain("McDonald's Restaurant");
        csvContent.Should().Contain("Shell Gas Station");
        csvContent.Should().Contain("Amazon Purchase");
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithEmptyResponse_CompletesSuccessfully()
    {
        // Arrange
        var emptyResponse = new TransactionApiResponse { Transactions = Array.Empty<RawTransaction>() };
        StartMockServer(JsonSerializer.Serialize(emptyResponse));

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithServerError_FailsGracefully()
    {
        // Arrange
        StartMockServer("", HttpStatusCode.InternalServerError);
        var pipeline = CreatePipeline();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            pipeline.ProcessTransactionsAsync("api/transactions"));
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var veryShortTimeoutSettings = _pipelineSettings with { TimeoutMinutes = 0 }; // Immediate timeout
        
        // Don't start mock server to cause timeout
        var pipeline = CreatePipeline(veryShortTimeoutSettings);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() => 
            pipeline.ProcessTransactionsAsync("api/transactions"));
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithComplexData_HandlesAllProcessingStages()
    {
        // Arrange
        var complexTransactions = new TransactionApiResponse
        {
            Transactions = new[]
            {
                new RawTransaction
                {
                    Id = "1",
                    Date = "2024-01-01",
                    Amount = "100.123",
                    Description = "  PURCHASE   AT   STARBUCKS***   COFFEE   "
                },
                new RawTransaction
                {
                    Id = "2",
                    Date = "2024-01-02",
                    Amount = "-50.99",
                    Description = "PAYMENT DEBIT NETFLIX SUBSCRIPTION"
                }
            }
        };

        StartMockServer(JsonSerializer.Serialize(complexTransactions));

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");

        // Assert
        result.Success.Should().BeTrue();

        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        csvFiles.Should().HaveCount(1);

        var csvContent = await File.ReadAllTextAsync(csvFiles[0]);
        
        // Verify processing occurred
        csvContent.Should().Contain("Starbucks Coffee"); // Cleaned description
        csvContent.Should().Contain("Netflix Subscription"); // Cleaned description
        csvContent.Should().Contain("100.12"); // Rounded amount
        csvContent.Should().Contain("50.99"); // Positive amount
        csvContent.Should().Contain("Food & Dining"); // Categorized
        csvContent.Should().Contain("Entertainment"); // Categorized
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithLargeDataSet_ProcessesEfficiently()
    {
        // Arrange
        var largeTransactionSet = new TransactionApiResponse
        {
            Transactions = Enumerable.Range(1, 50).Select(i => new RawTransaction
            {
                Id = i.ToString(),
                Date = DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd"),
                Amount = (i * 10.50m).ToString("F2"),
                Description = $"Transaction {i} at Test Merchant"
            }).ToArray()
        };

        StartMockServer(JsonSerializer.Serialize(largeTransactionSet));

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");

        // Assert
        result.Success.Should().BeTrue();
        result.Duration.Should().BeLessThan(TimeSpan.FromMinutes(1));

        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        csvFiles.Should().HaveCountGreaterThan(0);

        var totalCsvContent = string.Join("", await Task.WhenAll(csvFiles.Select(File.ReadAllTextAsync)));
        var lines = totalCsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Should have header + 50 transactions
        lines.Length.Should().BeGreaterThan(50);
    }

    [Theory]
    [InlineData("McDonald's", "Food & Dining")]
    [InlineData("Shell Gas", "Transportation")]
    [InlineData("Amazon", "Shopping")]
    [InlineData("Electric Bill", "Utilities")]
    [InlineData("Netflix", "Entertainment")]
    public async Task ProcessTransactionsAsync_WithDifferentMerchants_CategorizesCorrectly(string merchantName, string expectedCategory)
    {
        // Arrange
        var transactions = new TransactionApiResponse
        {
            Transactions = new[]
            {
                new RawTransaction
                {
                    Id = "1",
                    Date = "2024-01-01",
                    Amount = "50.00",
                    Description = merchantName
                }
            }
        };

        StartMockServer(JsonSerializer.Serialize(transactions));

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");

        // Assert
        result.Success.Should().BeTrue();

        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        var csvContent = await File.ReadAllTextAsync(csvFiles[0]);
        csvContent.Should().Contain(expectedCategory);
    }

    private TransactionPipeline CreatePipeline(PipelineSettings? customPipelineSettings = null)
    {
        var settings = customPipelineSettings ?? _pipelineSettings;
        
        var httpClient = new HttpClient();
        var fetcherAgent = new TransactionFetcherAgent(httpClient, _apiSettings, 
            new Mock<ILogger<TransactionFetcherAgent>>().Object);
        
        var processorAgent = new TransactionProcessorAgent(
            new Mock<ILogger<TransactionProcessorAgent>>().Object);
        
        var enricherAgent = new EmailEnricherAgent(_graphSettings, 
            new Mock<ILogger<EmailEnricherAgent>>().Object);
        
        var categorizerAgent = new CategorizerAgent(_openAISettings, 
            new Mock<ILogger<CategorizerAgent>>().Object);
        
        var exporterAgent = new CsvExporterAgent(_exportSettings, 
            new Mock<ILogger<CsvExporterAgent>>().Object);

        return new TransactionPipeline(
            fetcherAgent,
            processorAgent,
            enricherAgent,
            categorizerAgent,
            exporterAgent,
            settings,
            _pipelineLoggerMock.Object);
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
        _mockServer?.Stop();
        _mockServer?.Close();
        
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}