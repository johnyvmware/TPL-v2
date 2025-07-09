using FluentAssertions;
using NUnit.Framework;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Tests.UnitTests;

[TestFixture]
public class BasicTests
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

    [Test]
    public void Transaction_WithRecord_ShouldCreateCorrectObject()
    {
        // Arrange
        var originalTransaction = new Transaction
        {
            Id = "original-id",
            Date = DateTime.Today,
            Amount = 50.00m,
            Description = "Original Description",
            Category = "Food",
            Status = ProcessingStatus.Fetched
        };

        // Act
        var modifiedTransaction = originalTransaction with 
        { 
            Amount = 75.00m, 
            Description = "Modified Description",
            Status = ProcessingStatus.Processed
        };

        // Assert
        modifiedTransaction.Id.Should().Be("original-id");
        modifiedTransaction.Amount.Should().Be(75.00m);
        modifiedTransaction.Description.Should().Be("Modified Description");
        modifiedTransaction.Category.Should().Be("Food");
        modifiedTransaction.Status.Should().Be(ProcessingStatus.Processed);
        
        // Original should be unchanged
        originalTransaction.Amount.Should().Be(50.00m);
        originalTransaction.Description.Should().Be("Original Description");
        originalTransaction.Status.Should().Be(ProcessingStatus.Fetched);
    }

    [TestCase(ProcessingStatus.Fetched)]
    [TestCase(ProcessingStatus.Processed)]
    [TestCase(ProcessingStatus.Failed)]
    public void Transaction_Status_ShouldAcceptAllValidValues(ProcessingStatus status)
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Id = "test-id",
            Date = DateTime.Today,
            Amount = 10.00m,
            Description = "Test",
            Status = status
        };

        // Assert
        transaction.Status.Should().Be(status);
    }

    [Test]
    public void Transaction_WithNullCategory_ShouldBeValid()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Id = "test-id",
            Date = DateTime.Today,
            Amount = 10.00m,
            Description = "Test",
            Category = null,
            Status = ProcessingStatus.Fetched
        };

        // Assert
        transaction.Category.Should().BeNull();
        transaction.Id.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void Transaction_WithEmptyCategory_ShouldBeValid()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Id = "test-id",
            Date = DateTime.Today,
            Amount = 10.00m,
            Description = "Test",
            Category = string.Empty,
            Status = ProcessingStatus.Fetched
        };

        // Assert
        transaction.Category.Should().BeEmpty();
    }
} 