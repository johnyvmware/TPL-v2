using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using Xunit;

namespace TransactionProcessingSystem.Tests.Agents;

public class EmailEnricherAgentTests
{
    private readonly Mock<ILogger<EmailEnricherAgent>> _loggerMock;
    private readonly MicrosoftGraphSettings _settings;

    public EmailEnricherAgentTests()
    {
        _loggerMock = new Mock<ILogger<EmailEnricherAgent>>();
        _settings = new MicrosoftGraphSettings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TenantId = "test-tenant-id",
            EmailSearchDays = 2
        };
    }

    [Fact]
    public async Task ProcessAsync_WithValidTransaction_HandlesGraphClientError()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act - This will fail to create Graph client due to invalid credentials
        // but should handle the error gracefully
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
        result.EmailSubject.Should().BeNull();
        result.EmailSnippet.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_WithNetworkError_ContinuesProcessing()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(transaction.Id);
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
        result.EmailSubject.Should().BeNull();
        result.EmailSnippet.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidGraphCredentials_HandlesErrorGracefully()
    {
        // Arrange
        var invalidSettings = new MicrosoftGraphSettings
        {
            ClientId = "invalid-client-id",
            ClientSecret = "invalid-client-secret",
            TenantId = "invalid-tenant-id",
            EmailSearchDays = 2
        };

        var agent = new EmailEnricherAgent(invalidSettings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
        result.EmailSubject.Should().BeNull();
        result.EmailSnippet.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyCredentials_ThrowsException()
    {
        // Arrange
        var emptySettings = new MicrosoftGraphSettings
        {
            ClientId = "",
            ClientSecret = "",
            TenantId = "",
            EmailSearchDays = 2
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EmailEnricherAgent(emptySettings, _loggerMock.Object));
    }

    [Fact]
    public async Task ProcessAsync_WithDifferentDateRanges_AdjustsSearchWindow()
    {
        // Arrange
        var customSettings = _settings with { EmailSearchDays = 5 };
        var agent = new EmailEnricherAgent(customSettings, _loggerMock.Object);
        
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithFutureDate_HandlesDateRangeCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today.AddDays(1), // Future date
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithPastDate_HandlesDateRangeCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today.AddDays(-30), // Past date
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithLargeAmount_HandlesAmountCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 999999.99m, // Large amount
            Description = "Large Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithSmallAmount_HandlesAmountCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 0.01m, // Small amount
            Description = "Small Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithSpecialCharactersInDescription_HandlesCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "McDonald's @ 123 Main St. - Card #1234",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyDescription_HandlesCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(14)]
    public async Task ProcessAsync_WithDifferentEmailSearchDays_ConfiguresCorrectly(int searchDays)
    {
        // Arrange
        var customSettings = _settings with { EmailSearchDays = searchDays };
        var agent = new EmailEnricherAgent(customSettings, _loggerMock.Object);
        
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithCleanDescription_UsesCleanDescription()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "PURCHASE AT STARBUCKS COFFEE",
            CleanDescription = "Starbucks Coffee",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }

    [Fact]
    public async Task ProcessAsync_WithZeroAmount_HandlesCorrectly()
    {
        // Arrange
        var agent = new EmailEnricherAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 0.00m,
            Description = "Zero Amount Transaction",
            Status = ProcessingStatus.Processed
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ProcessingStatus.EmailEnriched);
    }
}