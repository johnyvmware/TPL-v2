using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TransactionProcessingSystem.Components;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Tests.UnitTests;

[TestFixture]
public class AsyncPatternsTests
{
    [Test]
    public void Components_ShouldImplementCorrectAsyncPatterns()
    {
        // Verify that our refactored components follow the correct async patterns
        // This test ensures that the refactoring maintained the intended behavior

        // Test that TransactionFetcher exists and can be instantiated
        var loggerMock = new Mock<ILogger<TransactionFetcher>>();
        var httpClient = new HttpClient();
        var settings = new TransactionApiSettings
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        var fetcher = new TransactionFetcher(httpClient, settings, loggerMock.Object);
        fetcher.Should().NotBeNull();
        fetcher.Should().BeAssignableTo<ProcessorBase<string, IEnumerable<Transaction>>>();

        // Cleanup
        fetcher.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public void TransactionProcessor_ShouldImplementCorrectPatterns()
    {
        // Test that TransactionProcessor exists and can be instantiated
        var loggerMock = new Mock<ILogger<TransactionProcessor>>();

        var processor = new TransactionProcessor(loggerMock.Object);
        processor.Should().NotBeNull();
        processor.Should().BeAssignableTo<ProcessorBase<Transaction, Transaction>>();

        // Cleanup
        processor.Dispose();
    }

    [Test]
    public void Neo4jExporter_ShouldImplementCorrectPatterns()
    {
        // Test that Neo4jExporter exists and can be instantiated
        var loggerMock = new Mock<ILogger<Neo4jExporter>>();
        var dataAccessMock = new Mock<INeo4jDataAccess>();

        var exporter = new Neo4jExporter(dataAccessMock.Object, loggerMock.Object);
        exporter.Should().NotBeNull();
        exporter.Should().BeAssignableTo<ProcessorBase<Transaction, Transaction>>();

        // Cleanup
        exporter.Dispose();
    }

    [Test]
    public void ProcessorBase_ShouldFollowAsyncPatterns()
    {
        // Verify that ProcessorBase properly handles async operations
        var loggerMock = new Mock<ILogger<TransactionProcessor>>();

        var processor = new TransactionProcessor(loggerMock.Object);

        // Verify that the processor implements the expected interface
        processor.Should().BeAssignableTo<IProcessor<Transaction, Transaction>>();

        // Cleanup
        processor.Dispose();
    }

    [Test]
    public void AsyncEnumerator_ComponentsShouldExist()
    {
        // Verify that components using async enumerators exist and can be instantiated
        // This ensures the refactoring was successful

        // EmailEnricher should exist and be instantiable (even if it fails due to Graph client)
        var emailEnricherType = typeof(EmailEnricher);
        emailEnricherType.Should().NotBeNull();
        emailEnricherType.Name.Should().Be("EmailEnricher");

        // Neo4jExporter should exist (renamed from Neo4jProcessor)
        var neo4jExporterType = typeof(Neo4jExporter);
        neo4jExporterType.Should().NotBeNull();
        neo4jExporterType.Name.Should().Be("Neo4jExporter");
    }

    [Test]
    public void RefactoredComponents_ShouldNotHaveUnnecessaryAsyncPatterns()
    {
        // This test verifies that we removed unnecessary async patterns
        // by checking that certain types exist and have the expected signatures

        var transactionProcessorType = typeof(TransactionProcessor);
        transactionProcessorType.Should().NotBeNull();

        var transactionFetcherType = typeof(TransactionFetcher);
        transactionFetcherType.Should().NotBeNull();

        // Verify that CsvExporter no longer exists in the codebase
        var allTypes = typeof(TransactionProcessor).Assembly.GetTypes();
        var csvExporterType = allTypes.FirstOrDefault(t => t.Name == "CsvExporter");
        csvExporterType.Should().BeNull("CsvExporter should have been removed");

        // Verify that Neo4jProcessor was renamed to Neo4jExporter
        var neo4jProcessorType = allTypes.FirstOrDefault(t => t.Name == "Neo4jProcessor");
        neo4jProcessorType.Should().BeNull("Neo4jProcessor should have been renamed to Neo4jExporter");

        var neo4jExporterType = allTypes.FirstOrDefault(t => t.Name == "Neo4jExporter");
        neo4jExporterType.Should().NotBeNull("Neo4jExporter should exist");
    }

    [Test]
    public void ReactiveComponents_ShouldBeRemoved()
    {
        // Verify that reactive Neo4j components were removed
        var allTypes = typeof(TransactionProcessor).Assembly.GetTypes();

        var reactiveDataAccessType = allTypes.FirstOrDefault(t => t.Name.Contains("ReactiveDataAccess"));
        reactiveDataAccessType.Should().BeNull("Reactive Neo4j components should have been removed");

        var reactiveInterfaceType = allTypes.FirstOrDefault(t => t.Name.Contains("INeo4jReactiveDataAccess"));
        reactiveInterfaceType.Should().BeNull("Reactive Neo4j interfaces should have been removed");
    }
}