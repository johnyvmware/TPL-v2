using FluentAssertions;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace TransactionProcessingSystem.Tests;

public class SimpleTests
{
    [Fact]
    public void Transaction_Constructor_CreatesValidTransaction()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Fetched
        };

        // Assert
        transaction.Id.Should().Be("1");
        transaction.Amount.Should().Be(100.50m);
        transaction.Description.Should().Be("Test Transaction");
        transaction.Status.Should().Be(ProcessingStatus.Fetched);
    }

    [Fact]
    public void MockTransactionApiService_Constructor_InitializesCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MockTransactionApiService>>();
        var baseUrl = "http://localhost:5001";

        // Act
        var service = new MockTransactionApiService(mockLogger.Object, baseUrl);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void ProcessingStatus_Enum_HasExpectedValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<ProcessingStatus>();

        // Assert
        values.Should().Contain(ProcessingStatus.Fetched);
        values.Should().Contain(ProcessingStatus.Processed);
        values.Should().Contain(ProcessingStatus.EmailEnriched);
        values.Should().Contain(ProcessingStatus.Categorized);
        values.Should().Contain(ProcessingStatus.Exported);
        values.Should().HaveCount(5);
    }

    [Fact]
    public void RawTransaction_Record_CanBeCreated()
    {
        // Arrange & Act
        var rawTransaction = new RawTransaction
        {
            Id = "test-id",
            Date = "2023-01-01",
            Amount = "100.50",
            Description = "Test Description"
        };

        // Assert
        rawTransaction.Id.Should().Be("test-id");
        rawTransaction.Date.Should().Be("2023-01-01");
        rawTransaction.Amount.Should().Be("100.50");
        rawTransaction.Description.Should().Be("Test Description");
    }

    [Fact]
    public void EmailMatch_Record_CanBeCreated()
    {
        // Arrange
        var now = DateTime.Now;

        // Act
        var emailMatch = new EmailMatch
        {
            Subject = "Test Subject",
            Snippet = "Test Snippet",
            ReceivedDateTime = now
        };

        // Assert
        emailMatch.Subject.Should().Be("Test Subject");
        emailMatch.Snippet.Should().Be("Test Snippet");
        emailMatch.ReceivedDateTime.Should().Be(now);
    }
}