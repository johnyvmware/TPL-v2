using FluentAssertions;
using TransactionProcessingSystem.Models;
using Xunit;

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
}