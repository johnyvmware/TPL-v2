using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Neo4j.Driver;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for the modernized Neo4jDataAccess implementation
/// Tests latest C# features including IAsyncEnumerable, ValueTask, and modern patterns
/// </summary>
public class Neo4jDataAccessTests : IDisposable
{
    private readonly Mock<IDriver> _mockDriver;
    private readonly Mock<IAsyncSession> _mockSession;
    private readonly Mock<IAsyncTransaction> _mockTransaction;
    private readonly Mock<IResultCursor> _mockCursor;
    private readonly Mock<IRecord> _mockRecord;
    private readonly Mock<ILogger<Neo4jDataAccess>> _mockLogger;
    private readonly IOptions<Neo4jSettings> _options;
    private readonly Neo4jDataAccess _dataAccess;

    public Neo4jDataAccessTests()
    {
        _mockDriver = new Mock<IDriver>();
        _mockSession = new Mock<IAsyncSession>();
        _mockTransaction = new Mock<IAsyncTransaction>();
        _mockCursor = new Mock<IResultCursor>();
        _mockRecord = new Mock<IRecord>();
        _mockLogger = new Mock<ILogger<Neo4jDataAccess>>();

        var settings = new Neo4jSettings
        {
            ConnectionUri = "neo4j://localhost:7687",
            Username = "neo4j",
            Password = "password",
            Database = "test",
            MaxConnectionPoolSize = 10,
            ConnectionTimeoutSeconds = 30
        };

        _options = Options.Create(settings);

        // Setup mock driver to return mock session
        _mockDriver
            .Setup(d => d.AsyncSession(It.IsAny<Action<SessionConfigBuilder>>()))
            .Returns(_mockSession.Object);

        _dataAccess = new Neo4jDataAccess(_mockDriver.Object, _options, _mockLogger.Object);
    }

    [Fact]
    public async Task VerifyConnectivityAsync_ShouldReturnTrue_WhenConnectionSucceeds()
    {
        // Arrange
        _mockRecord.Setup(r => r["test"]).Returns(new Mock<IValue>().Object);
        _mockRecord.Setup(r => r["test"].As<int>()).Returns(1);

        _mockCursor.Setup(c => c.SingleAsync()).ReturnsAsync(_mockRecord.Object);

        _mockTransaction
            .Setup(t => t.RunAsync("RETURN 1 AS test"))
            .ReturnsAsync(_mockCursor.Object);

        _mockSession
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncTransaction, Task<int>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<int>>, Action<TransactionConfigBuilder>>((func, config) => func(_mockTransaction.Object));

        // Act
        var result = await _dataAccess.VerifyConnectivityAsync();

        // Assert
        result.Should().BeTrue();
        _mockDriver.Verify(d => d.AsyncSession(It.IsAny<Action<SessionConfigBuilder>>()), Times.Once);
    }

    [Fact]
    public async Task VerifyConnectivityAsync_ShouldReturnFalse_WhenConnectionFails()
    {
        // Arrange
        _mockSession
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncTransaction, Task<int>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ThrowsAsync(new ServiceUnavailableException("Connection failed"));

        // Act
        var result = await _dataAccess.VerifyConnectivityAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpsertTransactionAsync_ShouldReturnTransactionId_WhenSuccessful()
    {
        // Arrange
        var transaction = CreateSampleTransaction();
        var expectedTransactionId = transaction.Id;

        _mockRecord.Setup(r => r["transactionId"]).Returns(new Mock<IValue>().Object);
        _mockRecord.Setup(r => r["transactionId"].As<string>()).Returns(expectedTransactionId);

        _mockCursor.Setup(c => c.SingleAsync()).ReturnsAsync(_mockRecord.Object);

        _mockTransaction
            .Setup(t => t.RunAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(_mockCursor.Object);

        _mockSession
            .Setup(s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncTransaction, Task<string>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<string>>, Action<TransactionConfigBuilder>>((func, config) => func(_mockTransaction.Object));

        // Act
        var result = await _dataAccess.UpsertTransactionAsync(transaction);

        // Assert
        result.Should().Be(expectedTransactionId);
        _mockTransaction.Verify(t => t.RunAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpsertTransactionsAsync_ShouldStreamResults_UsingIAsyncEnumerable()
    {
        // Arrange
        var transactions = CreateSampleTransactions();
        var results = new List<TransactionResult>();

        SetupUpsertMocks();

        // Act
        await foreach (var result in _dataAccess.UpsertTransactionsAsync(transactions.ToAsyncEnumerable()))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        results.Select(r => r.TransactionId).Should().BeEquivalentTo(transactions.Select(t => t.Id));
    }

    [Fact]
    public async Task FindSimilarTransactionsAsync_ShouldStreamSimilarTransactions_UsingModernPatterns()
    {
        // Arrange
        var referenceTransaction = CreateSampleTransaction();
        var similarTransactions = new List<Transaction>();

        SetupSimilarTransactionMocks();

        // Act
        await foreach (var similar in _dataAccess.FindSimilarTransactionsAsync(referenceTransaction))
        {
            similarTransactions.Add(similar);
        }

        // Assert
        similarTransactions.Should().HaveCount(2);
        similarTransactions.Should().AllSatisfy(t =>
        {
            t.Id.Should().NotBeNullOrEmpty();
            t.Amount.Should().BeGreaterThan(0);
            t.Description.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetTransactionAnalyticsAsync_ShouldReturnStronglyTypedAnalytics_WithModernRecords()
    {
        // Arrange
        SetupAnalyticsMocks();

        // Act
        var analytics = await _dataAccess.GetTransactionAnalyticsAsync();

        // Assert
        analytics.Should().NotBeNull();
        analytics.TotalTransactions.Should().Be(100);
        analytics.TotalAmount.Should().Be(50000.00m);
        analytics.AverageAmount.Should().Be(500.00m);
        analytics.Categories.Should().HaveCount(5);
        analytics.TopCategories.Should().HaveCount(3);
        analytics.DateRange.Should().NotBeNull();
        analytics.Relationships.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldStreamResults_UsingIAsyncEnumerable()
    {
        // Arrange
        var cypher = "MATCH (n) RETURN n LIMIT 5";
        var results = new List<IDictionary<string, object>>();

        SetupCustomQueryMocks();

        // Act
        await foreach (var result in _dataAccess.ExecuteQueryAsync(cypher))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.Should().ContainKey("id"));
    }

    [Fact]
    public async Task GetGraphStatisticsAsync_ShouldStreamStatistics_WithModernRecordStructs()
    {
        // Arrange
        var statistics = new List<GraphStatistic>();

        SetupGraphStatisticsMocks();

        // Act
        await foreach (var statistic in _dataAccess.GetGraphStatisticsAsync())
        {
            statistics.Add(statistic);
        }

        // Assert
        statistics.Should().HaveCount(4); // 2 nodes + 2 relationships
        statistics.Should().Contain(s => s.Type == "Node");
        statistics.Should().Contain(s => s.Type == "Relationship");
        statistics.Should().AllSatisfy(s => s.Count.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task InitializeDatabaseAsync_ShouldExecuteConstraintsAndIndexes_WithModernCollectionSyntax()
    {
        // Arrange
        var executedStatements = new List<string>();

        _mockTransaction
            .Setup(t => t.RunAsync(It.IsAny<string>()))
            .Callback<string>(statement => executedStatements.Add(statement))
            .ReturnsAsync(_mockCursor.Object);

        _mockSession
            .Setup(s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncTransaction, Task<Task>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<Task>>, Action<TransactionConfigBuilder>>((func, config) => func(_mockTransaction.Object));

        // Act
        await _dataAccess.InitializeDatabaseAsync();

        // Assert
        executedStatements.Should().HaveCountGreaterThan(5);
        executedStatements.Should().Contain(s => s.Contains("CREATE CONSTRAINT"));
        executedStatements.Should().Contain(s => s.Contains("CREATE INDEX"));
        executedStatements.Should().Contain(s => s.Contains("CREATE FULLTEXT INDEX"));
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeDriver_WithModernAsyncDisposalPattern()
    {
        // Act
        await _dataAccess.DisposeAsync();

        // Assert
        _mockDriver.Verify(d => d.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData("", "Unknown")]
    [InlineData(null, "Unknown")]
    [InlineData("Food & Dining", "Food & Dining")]
    public async Task UpsertTransactionAsync_ShouldHandleNullableCategories_WithModernNullHandling(string? category, string expectedCategory)
    {
        // Arrange
        var transaction = CreateSampleTransaction() with { Category = category };

        SetupUpsertMocks();

        // Act
        var result = await _dataAccess.UpsertTransactionAsync(transaction);

        // Assert
        result.Should().NotBeNullOrEmpty();
        _mockTransaction.Verify(t => t.RunAsync(
            It.Is<string>(cypher => cypher.Contains("MERGE (category:Category")),
            It.Is<object>(param => param.ToString()!.Contains(expectedCategory))), Times.Once);
    }

    // Helper methods using modern C# patterns

    private static Transaction CreateSampleTransaction() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Date = DateTime.UtcNow,
        Amount = 123.45m,
        Description = "Test transaction",
        Category = "Testing",
        Status = ProcessingStatus.Fetched
    };

    private static List<Transaction> CreateSampleTransactions() =>
    [
        CreateSampleTransaction(),
        CreateSampleTransaction() with { Id = Guid.NewGuid().ToString(), Amount = 234.56m },
        CreateSampleTransaction() with { Id = Guid.NewGuid().ToString(), Amount = 345.67m }
    ];

    private void SetupUpsertMocks()
    {
        _mockRecord.Setup(r => r["transactionId"]).Returns(new Mock<IValue>().Object);
        _mockRecord.Setup(r => r["transactionId"].As<string>()).Returns(() => Guid.NewGuid().ToString());

        _mockCursor.Setup(c => c.SingleAsync()).ReturnsAsync(_mockRecord.Object);

        _mockTransaction
            .Setup(t => t.RunAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(_mockCursor.Object);

        _mockSession
            .Setup(s => s.ExecuteWriteAsync(It.IsAny<Func<IAsyncTransaction, Task<string>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<string>>, Action<TransactionConfigBuilder>>((func, config) => func(_mockTransaction.Object));
    }

    private void SetupSimilarTransactionMocks()
    {
        var mockRecords = new[]
        {
            CreateMockRecord("similar-1", 120.00, "Similar transaction 1"),
            CreateMockRecord("similar-2", 125.00, "Similar transaction 2")
        };

        var recordQueue = new Queue<IRecord>(mockRecords);

        _mockCursor.Setup(c => c.FetchAsync()).ReturnsAsync(() => recordQueue.Count > 0);
        _mockCursor.Setup(c => c.Current).Returns(() => recordQueue.Count > 0 ? recordQueue.Dequeue() : null!);

        _mockSession
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncTransaction, Task<List<IDictionary<string, object>>>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(mockRecords.Select(r => new Dictionary<string, object>
            {
                ["id"] = r["id"].As<string>(),
                ["amount"] = r["amount"].As<double>(),
                ["description"] = r["description"].As<string>(),
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["status"] = ProcessingStatus.Processed.ToString()
            } as IDictionary<string, object>).ToList());
    }

    private Mock<IRecord> CreateMockRecord(string id, double amount, string description)
    {
        var mockRecord = new Mock<IRecord>();
        var mockValue = new Mock<IValue>();

        mockRecord.Setup(r => r.TryGetValue("id", out It.Ref<object>.IsAny)).Returns(true);
        mockRecord.Setup(r => r.TryGetValue("id", out It.Ref<object>.IsAny)).Callback((string key, out object value) => value = id);

        mockRecord.Setup(r => r["id"]).Returns(mockValue.Object);
        mockRecord.Setup(r => r["id"].As<string>()).Returns(id);

        mockRecord.Setup(r => r["amount"]).Returns(mockValue.Object);
        mockRecord.Setup(r => r["amount"].As<double>()).Returns(amount);

        mockRecord.Setup(r => r["description"]).Returns(mockValue.Object);
        mockRecord.Setup(r => r["description"].As<string>()).Returns(description);

        return mockRecord;
    }

    private void SetupAnalyticsMocks()
    {
        var analyticsData = new Dictionary<string, object>
        {
            ["totalTransactions"] = 100L,
            ["totalAmount"] = 50000.0,
            ["averageAmount"] = 500.0,
            ["minAmount"] = 10.0,
            ["maxAmount"] = 2000.0,
            ["categories"] = new List<object> { "Food", "Transport", "Shopping", "Bills", "Entertainment" },
            ["uniqueCategories"] = 5,
            ["dateRange"] = new Dictionary<string, object>
            {
                ["earliest"] = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
                ["latest"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["uniqueDays"] = 30,
                ["uniqueMonths"] = 1,
                ["uniqueYears"] = 1
            },
            ["relationships"] = new Dictionary<string, object>
            {
                ["categorySimilarities"] = 25L,
                ["amountSimilarities"] = 15L
            },
            ["weekendTransactions"] = 20L,
            ["topCategories"] = new List<object>
            {
                new Dictionary<string, object> { ["category"] = "Food", ["count"] = 30L },
                new Dictionary<string, object> { ["category"] = "Transport", ["count"] = 25L },
                new Dictionary<string, object> { ["category"] = "Shopping", ["count"] = 20L }
            }
        };

        _mockRecord.Setup(r => r["analytics"]).Returns(new Mock<IValue>().Object);
        _mockRecord.Setup(r => r["analytics"].As<IDictionary<string, object>>()).Returns(analyticsData);

        _mockCursor.Setup(c => c.SingleAsync()).ReturnsAsync(_mockRecord.Object);

        _mockTransaction
            .Setup(t => t.RunAsync(It.IsAny<string>()))
            .ReturnsAsync(_mockCursor.Object);

        _mockSession
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncTransaction, Task<TransactionAnalytics>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .Returns<Func<IAsyncTransaction, Task<TransactionAnalytics>>, Action<TransactionConfigBuilder>>((func, config) => func(_mockTransaction.Object));
    }

    private void SetupCustomQueryMocks()
    {
        var mockResults = Enumerable.Range(1, 5)
            .Select(i => new Dictionary<string, object> { ["id"] = $"node-{i}", ["name"] = $"Node {i}" })
            .Cast<IDictionary<string, object>>()
            .ToList();

        _mockSession
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncTransaction, Task<List<IDictionary<string, object>>>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(mockResults);
    }

    private void SetupGraphStatisticsMocks()
    {
        var nodeStats = new[]
        {
            new Dictionary<string, object> { ["label"] = "Transaction", ["count"] = 100L },
            new Dictionary<string, object> { ["label"] = "Category", ["count"] = 10L }
        };

        var relationshipStats = new[]
        {
            new Dictionary<string, object> { ["relationshipType"] = "BELONGS_TO_CATEGORY", ["count"] = 100L },
            new Dictionary<string, object> { ["relationshipType"] = "SIMILAR_AMOUNT", ["count"] = 25L }
        };

        var allStats = nodeStats.Concat(relationshipStats).Cast<IDictionary<string, object>>().ToList();

        _mockSession
            .Setup(s => s.ExecuteReadAsync(It.IsAny<Func<IAsyncTransaction, Task<List<IDictionary<string, object>>>>>(), It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(allStats);
    }

    public void Dispose()
    {
        _dataAccess?.DisposeAsync().AsTask().Wait();
    }
}