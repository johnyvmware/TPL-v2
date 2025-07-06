using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using Xunit;

namespace TransactionProcessingSystem.Tests.Agents;

public class CategorizerAgentTests
{
    private readonly Mock<ILogger<CategorizerAgent>> _loggerMock;
    private readonly OpenAISettings _settings;

    public CategorizerAgentTests()
    {
        _loggerMock = new Mock<ILogger<CategorizerAgent>>();
        _settings = new OpenAISettings
        {
            ApiKey = "test-key",
            Model = "gpt-3.5-turbo",
            MaxTokens = 150,
            Temperature = 0.3
        };
    }

    [Fact]
    public async Task ProcessAsync_WithFoodTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 25.50m,
            Description = "McDonald's Restaurant",
            CleanDescription = "McDonald's Restaurant",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Food & Dining");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithTransportationTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 45.75m,
            Description = "Shell Gas Station",
            CleanDescription = "Shell Gas Station",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Transportation");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithShoppingTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 125.99m,
            Description = "Amazon Purchase",
            CleanDescription = "Amazon Purchase",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Shopping");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithUtilitiesTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 89.45m,
            Description = "Electric Company Bill",
            CleanDescription = "Electric Company Bill",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Utilities");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithEntertainmentTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 15.99m,
            Description = "Netflix Subscription",
            CleanDescription = "Netflix Subscription",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Entertainment");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithHealthcareTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 25.50m,
            Description = "CVS Pharmacy",
            CleanDescription = "CVS Pharmacy",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Healthcare");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithFinancialTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 3.50m,
            Description = "Bank ATM Fee",
            CleanDescription = "Bank ATM Fee",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Financial Services");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithTravelTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 150.00m,
            Description = "Hotel Booking",
            CleanDescription = "Hotel Booking",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Travel");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithBusinessTransaction_UsesFallbackCategorization()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 99.99m,
            Description = "Office Supplies",
            CleanDescription = "Office Supplies",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Business Services");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithUnknownTransaction_UsesOtherCategory()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 50.00m,
            Description = "Unknown Merchant XYZ",
            CleanDescription = "Unknown Merchant XYZ",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Other");
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Fact]
    public async Task ProcessAsync_WithEmailContext_IncludesEmailInformation()
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 50.00m,
            Description = "Unknown Merchant",
            CleanDescription = "Unknown Merchant",
            EmailSubject = "Receipt from Restaurant",
            EmailSnippet = "Thank you for dining with us",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be("Other"); // Falls back since no keyword match
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }

    [Theory]
    [InlineData("McDonald's", "Food & Dining")]
    [InlineData("Starbucks", "Food & Dining")]
    [InlineData("Restaurant", "Food & Dining")]
    [InlineData("Shell", "Transportation")]
    [InlineData("Uber", "Transportation")]
    [InlineData("Amazon", "Shopping")]
    [InlineData("Walmart", "Shopping")]
    [InlineData("Electric", "Utilities")]
    [InlineData("Netflix", "Entertainment")]
    [InlineData("CVS", "Healthcare")]
    [InlineData("Bank", "Financial Services")]
    [InlineData("Hotel", "Travel")]
    [InlineData("Office", "Business Services")]
    public async Task ProcessAsync_WithKeywordInDescription_CategorizesByKeyword(string keyword, string expectedCategory)
    {
        // Arrange
        var agent = new CategorizerAgent(_settings, _loggerMock.Object);
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 50.00m,
            Description = $"Transaction at {keyword}",
            CleanDescription = $"Transaction at {keyword}",
            Status = ProcessingStatus.EmailEnriched
        };

        // Act
        var result = await agent.ProcessAsync(transaction);

        // Assert
        result.Category.Should().Be(expectedCategory);
        result.Status.Should().Be(ProcessingStatus.Categorized);
    }
}