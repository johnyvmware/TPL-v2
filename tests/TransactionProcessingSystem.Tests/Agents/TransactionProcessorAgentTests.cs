using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Agents;
using TransactionProcessingSystem.Models;
using Xunit;

namespace TransactionProcessingSystem.Tests.Agents;

public class TransactionProcessorAgentTests
{
    private readonly Mock<ILogger<TransactionProcessorAgent>> _loggerMock;
    private readonly TransactionProcessorAgent _agent;

    public TransactionProcessorAgentTests()
    {
        _loggerMock = new Mock<ILogger<TransactionProcessorAgent>>();
        _agent = new TransactionProcessorAgent(_loggerMock.Object);
    }

    [Fact]
    public async Task ProcessAsync_WithValidTransaction_CleansDescription()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "  PURCHASE   AT   STARBUCKS   COFFEE   ",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().Be("Starbucks Coffee");
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithSpecialCharacters_RemovesExcessiveSpecialChars()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "McDonald's*** Restaurant!!! #@#$%",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().Be("McDonald's Restaurant");
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithRedundantTerms_RemovesRedundantWords()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "PURCHASE PAYMENT DEBIT AMAZON STORE",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().Be("Amazon Store");
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithAbbreviations_KeepsAbbreviationsUppercase()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "atm withdrawal bank of america",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().Be("ATM Withdrawal Bank Of America");
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyDescription_ReturnsEmptyString()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "   ",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().BeEmpty();
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidDate_NormalizesToToday()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.MinValue,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.Date.Should().Be(DateTime.Today);
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithNegativeAmount_MakesPositive()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = -100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.Amount.Should().Be(100.50m);
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithPreciseAmount_RoundsToTwoDecimalPlaces()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.123456m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.Amount.Should().Be(100.12m);
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithMixedCaseWords_PreservesMixedCase()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = "McDonald's Restaurant",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().Be("McDonald's Restaurant");
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task ProcessAsync_WithDateWithTime_RemovesTimeComponent()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 1, 14, 30, 45);
        var transaction = new Transaction
        {
            Id = "1",
            Date = dateTime,
            Amount = 100.50m,
            Description = "Test Transaction",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.Date.Should().Be(new DateTime(2024, 1, 1));
        result.Status.Should().Be(ProcessingStatus.Processed);
    }

    [Theory]
    [InlineData("LLC", "LLC")]
    [InlineData("INC", "INC")]
    [InlineData("CO", "CO")]
    [InlineData("ltd", "LTD")]
    [InlineData("api", "API")]
    public async Task ProcessAsync_WithCommonAbbreviations_KeepsThemUppercase(string input, string expected)
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = "1",
            Date = DateTime.Today,
            Amount = 100.50m,
            Description = $"Company {input} Store",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var result = await _agent.ProcessAsync(transaction);

        // Assert
        result.CleanDescription.Should().Be($"Company {expected} Store");
        result.Status.Should().Be(ProcessingStatus.Processed);
    }
}