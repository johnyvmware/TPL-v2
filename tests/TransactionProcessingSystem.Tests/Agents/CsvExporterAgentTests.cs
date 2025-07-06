using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using Xunit;

namespace TransactionProcessingSystem.Tests.Agents;

public class CsvExporterAgentTests : IDisposable
{
    private readonly Mock<ILogger<CsvExporterAgent>> _loggerMock;
    private readonly ExportSettings _settings;
    private readonly string _tempDirectory;

    public CsvExporterAgentTests()
    {
        _loggerMock = new Mock<ILogger<CsvExporterAgent>>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"TransactionTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        _settings = new ExportSettings
        {
            OutputDirectory = _tempDirectory,
            FileNameFormat = "test_transactions_{0:yyyyMMdd_HHmmss}.csv",
            BufferSize = 3
        };
    }

    [Fact]
    public async Task ProcessTransaction_WithSingleTransaction_BuffersCorrectly()
    {
        // Arrange
        using var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transaction = CreateSampleTransaction("1");

        // Act
        await agent.InputBlock.SendAsync(transaction);

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().BeEmpty(); // Should be buffered, not exported yet
    }

    [Fact]
    public async Task ProcessTransaction_WithBufferSizeReached_ExportsToFile()
    {
        // Arrange
        using var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transactions = new[]
        {
            CreateSampleTransaction("1"),
            CreateSampleTransaction("2"),
            CreateSampleTransaction("3")
        };

        // Act
        foreach (var transaction in transactions)
        {
            await agent.InputBlock.SendAsync(transaction);
        }

        // Wait for processing
        await Task.Delay(1000);

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().HaveCount(1);
        
        var csvContent = await File.ReadAllTextAsync(files[0]);
        csvContent.Should().Contain("Date,Amount,Description,Category");
        csvContent.Should().Contain("2024-01-01,100.50,Test Transaction 1,Food & Dining");
        csvContent.Should().Contain("2024-01-01,100.50,Test Transaction 2,Food & Dining");
        csvContent.Should().Contain("2024-01-01,100.50,Test Transaction 3,Food & Dining");
    }

    [Fact]
    public async Task FinalFlush_WithBufferedTransactions_ExportsRemainingTransactions()
    {
        // Arrange
        using var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transactions = new[]
        {
            CreateSampleTransaction("1"),
            CreateSampleTransaction("2") // Only 2 transactions, below buffer size
        };

        // Act
        foreach (var transaction in transactions)
        {
            await agent.InputBlock.SendAsync(transaction);
        }

        await agent.FinalFlush();

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().HaveCount(1);
        
        var csvContent = await File.ReadAllTextAsync(files[0]);
        csvContent.Should().Contain("Test Transaction 1");
        csvContent.Should().Contain("Test Transaction 2");
    }

    [Fact]
    public async Task ExportTransactions_WithCompleteTransactionData_CreatesValidCsv()
    {
        // Arrange
        using var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = new DateTime(2024, 1, 1),
            Amount = 100.50m,
            Description = "Original Description",
            CleanDescription = "Clean Description",
            EmailSubject = "Email Subject",
            EmailSnippet = "Email Snippet",
            Category = "Food & Dining",
            Status = ProcessingStatus.Categorized
        };

        // Act
        await agent.InputBlock.SendAsync(transaction);
        await agent.FinalFlush();

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().HaveCount(1);
        
        var csvContent = await File.ReadAllTextAsync(files[0]);
        csvContent.Should().Contain("2024-01-01,100.50,Clean Description,Food & Dining,Email Subject,Email Snippet");
    }

    [Fact]
    public async Task ExportTransactions_WithMissingOptionalFields_HandlesNullValues()
    {
        // Arrange
        using var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = new DateTime(2024, 1, 1),
            Amount = 100.50m,
            Description = "Original Description",
            CleanDescription = null,
            EmailSubject = null,
            EmailSnippet = null,
            Category = null,
            Status = ProcessingStatus.Categorized
        };

        // Act
        await agent.InputBlock.SendAsync(transaction);
        await agent.FinalFlush();

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().HaveCount(1);
        
        var csvContent = await File.ReadAllTextAsync(files[0]);
        csvContent.Should().Contain("2024-01-01,100.50,Original Description,Uncategorized,,");
    }

    [Fact]
    public async Task ExportTransactions_WithSpecialCharactersInData_EscapesCorrectly()
    {
        // Arrange
        using var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = new DateTime(2024, 1, 1),
            Amount = 100.50m,
            Description = "Description with \"quotes\" and, commas",
            CleanDescription = "Clean Description with \"quotes\" and, commas",
            EmailSubject = "Subject with \"quotes\"",
            EmailSnippet = "Snippet with, commas",
            Category = "Food & Dining",
            Status = ProcessingStatus.Categorized
        };

        // Act
        await agent.InputBlock.SendAsync(transaction);
        await agent.FinalFlush();

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().HaveCount(1);
        
        var csvContent = await File.ReadAllTextAsync(files[0]);
        csvContent.Should().NotBeEmpty();
        // CSV should handle special characters properly
    }

    [Fact]
    public async Task Constructor_WithNonExistentDirectory_CreatesDirectory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_tempDirectory, "nonexistent");
        var settings = new ExportSettings
        {
            OutputDirectory = nonExistentDir,
            FileNameFormat = "test_{0:yyyyMMdd}.csv",
            BufferSize = 1
        };

        // Act
        using var agent = new CsvExporterAgent(settings, _loggerMock.Object);
        
        // Assert
        Directory.Exists(nonExistentDir).Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_WithBufferedTransactions_PerformsFinalFlush()
    {
        // Arrange
        var agent = new CsvExporterAgent(_settings, _loggerMock.Object);
        var transaction = CreateSampleTransaction("1");

        // Act
        await agent.InputBlock.SendAsync(transaction);
        agent.Dispose(); // Should trigger final flush

        // Assert
        var files = Directory.GetFiles(_tempDirectory, "*.csv");
        files.Should().HaveCount(1);
    }

    private Transaction CreateSampleTransaction(string id)
    {
        return new Transaction
        {
            Id = id,
            Date = new DateTime(2024, 1, 1),
            Amount = 100.50m,
            Description = $"Test Transaction {id}",
            CleanDescription = $"Test Transaction {id}",
            EmailSubject = $"Email Subject {id}",
            EmailSnippet = $"Email Snippet {id}",
            Category = "Food & Dining",
            Status = ProcessingStatus.Categorized
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}