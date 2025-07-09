using FluentAssertions;
using NUnit.Framework;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Tests;

[TestFixture]
public class SimpleTests
{
    [Test]
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