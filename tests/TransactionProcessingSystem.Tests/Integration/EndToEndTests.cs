using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Pipeline;
using TransactionProcessingSystem.Services;
using Xunit;

namespace TransactionProcessingSystem.Tests.Integration;

public class EndToEndTests : IDisposable
{
    private readonly string _tempDirectory;
    private IHost? _host;

    public EndToEndTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"E2ETests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task FullSystemIntegration_WithMockApi_ProcessesTransactionsEndToEnd()
    {
        // Arrange
        _host = CreateTestHost();
        
        // Act
        await _host.StartAsync();
        
        // Wait for mock API to start
        await Task.Delay(2000);
        
        // Get the pipeline and run processing
        var pipeline = _host.Services.GetRequiredService<TransactionPipeline>();
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        
        // Verify CSV files were created
        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        csvFiles.Should().HaveCount(1);
        
        var csvContent = await File.ReadAllTextAsync(csvFiles[0]);
        csvContent.Should().Contain("Date,Amount,Description,Category");
        csvContent.Should().NotBeEmpty();
        
        // Verify some expected transaction data
        csvContent.Should().Contain("McDonald's");
        csvContent.Should().Contain("Shell");
        csvContent.Should().Contain("Amazon");
        csvContent.Should().Contain("Netflix");
        
        // Verify categorization worked
        csvContent.Should().Contain("Food & Dining");
        csvContent.Should().Contain("Transportation");
        csvContent.Should().Contain("Shopping");
        csvContent.Should().Contain("Entertainment");
        
        await _host.StopAsync();
    }

    [Fact]
    public async Task MockApiService_GeneratesRealisticData()
    {
        // Arrange
        _host = CreateTestHost();
        
        // Act
        await _host.StartAsync();
        await Task.Delay(2000); // Wait for API to start
        
        // Test the mock API directly
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5000/api/transactions");
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var jsonContent = await response.Content.ReadAsStringAsync();
        jsonContent.Should().NotBeEmpty();
        jsonContent.Should().Contain("transactions");
        jsonContent.Should().Contain("id");
        jsonContent.Should().Contain("date");
        jsonContent.Should().Contain("amount");
        jsonContent.Should().Contain("description");
        
        await _host.StopAsync();
    }

    [Fact]
    public async Task Pipeline_HandlesApiFailure_Gracefully()
    {
        // Arrange
        _host = CreateTestHost();
        
        // Act
        await _host.StartAsync();
        
        var pipeline = _host.Services.GetRequiredService<TransactionPipeline>();
        
        // Try to process from a non-existent endpoint
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            pipeline.ProcessTransactionsAsync("api/nonexistent"));
        
        // Assert
        exception.Should().NotBeNull();
        
        await _host.StopAsync();
    }

    [Fact]
    public async Task Pipeline_WithRealDataProcessing_ProducesQualityOutput()
    {
        // Arrange
        _host = CreateTestHost();
        
        // Act
        await _host.StartAsync();
        await Task.Delay(2000);
        
        var pipeline = _host.Services.GetRequiredService<TransactionPipeline>();
        var result = await pipeline.ProcessTransactionsAsync("api/transactions");
        
        // Assert
        result.Success.Should().BeTrue();
        
        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        var csvContent = await File.ReadAllTextAsync(csvFiles[0]);
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Should have header + transactions
        lines.Length.Should().BeGreaterThan(1);
        
        // Verify header format
        lines[0].Should().Be("Date,Amount,Description,Category,Email Subject,Email Snippet");
        
        // Verify data quality - each transaction line should have all fields
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            fields.Length.Should().BeGreaterOrEqualTo(6);
            
            // Date should be in valid format
            DateTime.TryParse(fields[0], out _).Should().BeTrue();
            
            // Amount should be numeric
            decimal.TryParse(fields[1], out _).Should().BeTrue();
            
            // Description should not be empty
            fields[2].Should().NotBeEmpty();
            
            // Category should be one of the valid categories
            var validCategories = new[] { "Food & Dining", "Transportation", "Shopping", "Utilities", 
                "Entertainment", "Healthcare", "Education", "Travel", "Financial Services", 
                "Business Services", "Other" };
            validCategories.Should().Contain(fields[3]);
        }
        
        await _host.StopAsync();
    }

    [Fact]
    public async Task System_HandlesConcurrentRequests_Safely()
    {
        // Arrange
        _host = CreateTestHost();
        
        // Act
        await _host.StartAsync();
        await Task.Delay(2000);
        
        var pipeline = _host.Services.GetRequiredService<TransactionPipeline>();
        
        // Run multiple concurrent pipeline executions
        var tasks = Enumerable.Range(1, 3).Select(async i =>
        {
            var concurrentPipeline = _host.Services.GetRequiredService<TransactionPipeline>();
            return await concurrentPipeline.ProcessTransactionsAsync("api/transactions");
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        
        // Should have multiple CSV files
        var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
        csvFiles.Length.Should().BeGreaterThan(0);
        
        await _host.StopAsync();
    }

    private IHost CreateTestHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Use the same configuration as the main application but with test settings
                services.AddSingleton(new TransactionProcessingSystem.Configuration.OpenAISettings
                {
                    ApiKey = "test-key",
                    Model = "gpt-3.5-turbo"
                });
                
                services.AddSingleton(new TransactionProcessingSystem.Configuration.MicrosoftGraphSettings
                {
                    ClientId = "test-client-id",
                    ClientSecret = "test-client-secret",
                    TenantId = "test-tenant-id"
                });
                
                services.AddSingleton(new TransactionProcessingSystem.Configuration.TransactionApiSettings
                {
                    BaseUrl = "http://localhost:5000"
                });
                
                services.AddSingleton(new TransactionProcessingSystem.Configuration.ExportSettings
                {
                    OutputDirectory = _tempDirectory,
                    BufferSize = 5
                });
                
                services.AddSingleton(new TransactionProcessingSystem.Configuration.PipelineSettings
                {
                    BoundedCapacity = 10,
                    TimeoutMinutes = 2
                });
                
                // Add HTTP client
                services.AddHttpClient<TransactionProcessingSystem.Agents.TransactionFetcherAgent>();
                
                // Add agents
                services.AddTransient<TransactionProcessingSystem.Agents.TransactionFetcherAgent>();
                services.AddTransient<TransactionProcessingSystem.Agents.TransactionProcessorAgent>();
                services.AddTransient<TransactionProcessingSystem.Agents.EmailEnricherAgent>();
                services.AddTransient<TransactionProcessingSystem.Agents.CategorizerAgent>();
                services.AddTransient<TransactionProcessingSystem.Agents.CsvExporterAgent>();
                
                // Add pipeline
                services.AddTransient<TransactionPipeline>();
                
                // Add mock API service
                services.AddSingleton<MockTransactionApiService>();
                services.AddHostedService<MockTransactionApiService>(provider => 
                    provider.GetRequiredService<MockTransactionApiService>());
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
            })
            .Build();
    }

    public void Dispose()
    {
        _host?.Dispose();
        
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}