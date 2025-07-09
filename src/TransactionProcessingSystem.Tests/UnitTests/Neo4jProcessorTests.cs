using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Processors;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for the modernized Neo4jProcessor
/// Tests latest C# features including primary constructors, IAsyncEnumerable, and ValueTask patterns
/// </summary>
public class Neo4jProcessorTests
{
    private readonly Mock<INeo4jDataAccess> _mockDataAccess;
    private readonly Mock<ILogger<Neo4jProcessor>> _mockLogger;
    private readonly Neo4jProcessor _processor;

    public Neo4jProcessorTests()
    {
        _mockDataAccess = new Mock<INeo4jDataAccess>();
        _mockLogger = new Mock<ILogger<Neo4jProcessor>>();
        _processor = new Neo4jProcessor(_mockDataAccess.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessItemAsync_ShouldReturnProcessedTransaction_WhenSuccessful()
    {
        // Arrange
        var transaction = CreateSampleTransaction();
        var expectedTransactionId = transaction.Id;

        _mockDataAccess
            .Setup(d => d.UpsertTransactionAsync(transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactionId);

        // Act
        var result = await _processor.ProcessItemAsync(transaction, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(transaction.Id);
        result.Status.Should().Be(ProcessingStatus.Processed);
        _mockDataAccess.Verify(d => d.UpsertTransactionAsync(transaction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessItemAsync_ShouldReturnFailedTransaction_WhenExceptionOccurs()
    {
        // Arrange
        var transaction = CreateSampleTransaction();

        _mockDataAccess
            .Setup(d => d.UpsertTransactionAsync(transaction, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _processor.ProcessItemAsync(transaction, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(transaction.Id);
        result.Status.Should().Be(ProcessingStatus.Failed);
    }

    [Fact]
    public async Task ProcessTransactionsBatchAsync_ShouldStreamResults_UsingIAsyncEnumerable()
    {
        // Arrange
        var transactions = CreateSampleTransactions(3);
        var transactionResults = transactions.Select(t => new TransactionResult(t.Id, true)).ToAsyncEnumerable();

        _mockDataAccess
            .Setup(d => d.UpsertTransactionsAsync(It.IsAny<IAsyncEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
            .Returns(transactionResults);

        var results = new List<Transaction>();

        // Act
        await foreach (var result in _processor.ProcessTransactionsBatchAsync(transactions.ToAsyncEnumerable()))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Status.Should().Be(ProcessingStatus.Processed));
    }

    [Fact]
    public async Task AnalyzeTransactionPatternsAsync_ShouldReturnAnalytics_WithValueTaskPerformance()
    {
        // Arrange
        var expectedAnalytics = CreateSampleAnalytics();

        _mockDataAccess
            .Setup(d => d.GetTransactionAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnalytics);

        // Act
        var result = await _processor.AnalyzeTransactionPatternsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedAnalytics);
        _mockDataAccess.Verify(d => d.GetTransactionAnalyticsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindSimilarTransactionsAsync_ShouldStreamSimilarTransactions_WithEnumeratorCancellation()
    {
        // Arrange
        var referenceTransaction = CreateSampleTransaction();
        var similarTransactions = CreateSampleTransactions(2);

        _mockDataAccess
            .Setup(d => d.FindSimilarTransactionsAsync(referenceTransaction, It.IsAny<CancellationToken>()))
            .Returns(similarTransactions.ToAsyncEnumerable());

        var results = new List<Transaction>();
        using var cts = new CancellationTokenSource();

        // Act
        await foreach (var similar in _processor.FindSimilarTransactionsAsync(referenceTransaction, cts.Token))
        {
            results.Add(similar);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().BeEquivalentTo(similarTransactions);
    }

    [Fact]
    public async Task GetGraphStatisticsAsync_ShouldStreamStatistics_WithModernAsyncPatterns()
    {
        // Arrange
        var expectedStatistics = CreateSampleGraphStatistics();

        _mockDataAccess
            .Setup(d => d.GetGraphStatisticsAsync(It.IsAny<CancellationToken>()))
            .Returns(expectedStatistics.ToAsyncEnumerable());

        var results = new List<GraphStatistic>();

        // Act
        await foreach (var statistic in _processor.GetGraphStatisticsAsync())
        {
            results.Add(statistic);
        }

        // Assert
        results.Should().HaveCount(expectedStatistics.Count);
        results.Should().BeEquivalentTo(expectedStatistics);
    }

    [Fact]
    public async Task ExecuteCustomAnalyticsAsync_ShouldStreamResults_WithoutYieldInTryCatch()
    {
        // Arrange
        var cypherQuery = "MATCH (n) RETURN n.id as id LIMIT 5";
        var expectedResults = CreateSampleQueryResults(5);

        _mockDataAccess
            .Setup(d => d.ExecuteQueryAsync(cypherQuery, null, It.IsAny<CancellationToken>()))
            .Returns(expectedResults.ToAsyncEnumerable());

        var results = new List<IDictionary<string, object>>();

        // Act
        await foreach (var result in _processor.ExecuteCustomAnalyticsAsync(cypherQuery))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(5);
        results.Should().BeEquivalentTo(expectedResults);
    }

    [Fact]
    public async Task ExecuteCustomAnalyticsAsync_ShouldHandleExceptions_WithoutYieldInTryCatch()
    {
        // Arrange
        var cypherQuery = "INVALID CYPHER QUERY";

        _mockDataAccess
            .Setup(d => d.ExecuteQueryAsync(cypherQuery, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid query"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var result in _processor.ExecuteCustomAnalyticsAsync(cypherQuery))
            {
                // Should not reach here
            }
        });

        exception.Message.Should().Be("Invalid query");
    }

    [Fact]
    public async Task InitializeAsync_ShouldVerifyConnectivityAndInitializeDatabase_WithModernAsyncPatterns()
    {
        // Arrange
        _mockDataAccess
            .Setup(d => d.VerifyConnectivityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockDataAccess
            .Setup(d => d.InitializeDatabaseAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _processor.InitializeAsync();

        // Assert
        _mockDataAccess.Verify(d => d.VerifyConnectivityAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockDataAccess.Verify(d => d.InitializeDatabaseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ShouldThrowException_WhenConnectivityFails()
    {
        // Arrange
        _mockDataAccess
            .Setup(d => d.VerifyConnectivityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _processor.InitializeAsync().AsTask());

        exception.Message.Should().Contain("Failed to connect to Neo4j database");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ProcessTransactionsBatchAsync_ShouldHandleDifferentBatchSizes_WithModernCollectionSyntax(int batchSize)
    {
        // Arrange
        var transactions = CreateSampleTransactions(batchSize);
        var transactionResults = transactions.Select(t => new TransactionResult(t.Id, true)).ToAsyncEnumerable();

        _mockDataAccess
            .Setup(d => d.UpsertTransactionsAsync(It.IsAny<IAsyncEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
            .Returns(transactionResults);

        var results = new List<Transaction>();

        // Act
        await foreach (var result in _processor.ProcessTransactionsBatchAsync(transactions.ToAsyncEnumerable()))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(batchSize);
        results.Should().AllSatisfy(r => r.Status.Should().Be(ProcessingStatus.Processed));
    }

    [Fact]
    public async Task ProcessItemAsync_ShouldLogDebugAndTrace_WithStructuredLogging()
    {
        // Arrange
        var transaction = CreateSampleTransaction();

        _mockDataAccess
            .Setup(d => d.UpsertTransactionAsync(transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Id);

        // Act
        await _processor.ProcessItemAsync(transaction);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully stored transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionsBatchAsync_ShouldHandleFailedTransactions_WithProperStatusMapping()
    {
        // Arrange
        var transactions = CreateSampleTransactions(3);
        var transactionResults = new[]
        {
            new TransactionResult(transactions[0].Id, true),
            new TransactionResult(transactions[1].Id, false, "Database error"),
            new TransactionResult(transactions[2].Id, true)
        }.ToAsyncEnumerable();

        _mockDataAccess
            .Setup(d => d.UpsertTransactionsAsync(It.IsAny<IAsyncEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
            .Returns(transactionResults);

        var results = new List<Transaction>();

        // Act
        await foreach (var result in _processor.ProcessTransactionsBatchAsync(transactions.ToAsyncEnumerable()))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Status.Should().Be(ProcessingStatus.Processed);
        results[1].Status.Should().Be(ProcessingStatus.Failed);
        results[2].Status.Should().Be(ProcessingStatus.Processed);
    }

    [Fact]
    public async Task CancellationToken_ShouldBePropagatedThroughAsyncEnumerables()
    {
        // Arrange
        var transactions = CreateSampleTransactions(10);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50)); // Cancel quickly

        _mockDataAccess
            .Setup(d => d.UpsertTransactionsAsync(It.IsAny<IAsyncEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
            .Returns(async IAsyncEnumerable<TransactionResult> GetResults()
            {
                foreach (var transaction in transactions)
                {
                    await Task.Delay(20, cts.Token); // Simulate work
                    yield return new TransactionResult(transaction.Id, true);
                }
            });

        var results = new List<Transaction>();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var result in _processor.ProcessTransactionsBatchAsync(transactions.ToAsyncEnumerable(), cts.Token))
            {
                results.Add(result);
            }
        });

        exception.Should().NotBeNull();
    }

    // Helper methods using modern C# patterns

    private static Transaction CreateSampleTransaction() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Date = DateTime.UtcNow,
        Amount = Random.Shared.Next(10, 1000),
        Description = $"Test transaction {Random.Shared.Next(1000, 9999)}",
        Category = new[] { "Food", "Transport", "Shopping" }[Random.Shared.Next(3)],
        Status = ProcessingStatus.Fetched
    };

    private static List<Transaction> CreateSampleTransactions(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateSampleTransaction())
            .ToList();

    private static TransactionAnalytics CreateSampleAnalytics() => new()
    {
        TotalTransactions = 100,
        TotalAmount = 50000.00m,
        AverageAmount = 500.00m,
        MinAmount = 10.00m,
        MaxAmount = 2000.00m,
        Categories = ["Food", "Transport", "Shopping", "Bills", "Entertainment"],
        UniqueCategories = 5,
        DateRange = new DateRange(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            30,
            1,
            1),
        Relationships = new RelationshipStats(25, 15),
        WeekendTransactions = 20,
        TopCategories =
        [
            new CategoryBreakdown("Food", 30),
            new CategoryBreakdown("Transport", 25),
            new CategoryBreakdown("Shopping", 20)
        ]
    };

    private static List<GraphStatistic> CreateSampleGraphStatistics() =>
    [
        new GraphStatistic("Node", "Transaction", 100),
        new GraphStatistic("Node", "Category", 10),
        new GraphStatistic("Relationship", "BELONGS_TO_CATEGORY", 100),
        new GraphStatistic("Relationship", "SIMILAR_AMOUNT", 25)
    ];

    private static List<IDictionary<string, object>> CreateSampleQueryResults(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Dictionary<string, object>
            {
                ["id"] = $"node-{i}",
                ["name"] = $"Node {i}",
                ["value"] = Random.Shared.Next(1, 100)
            })
            .Cast<IDictionary<string, object>>()
            .ToList();
}