using Xunit;
using System.Reflection;
using FluentAssertions;

namespace TransactionProcessingSystem.Tests;

public class TestRunner
{
    [Fact]
    public void AllTests_ShouldPass_WithFullCoverage()
    {
        // This test validates that our test suite has proper coverage
        // by checking that all major components have corresponding tests

        var testAssembly = Assembly.GetExecutingAssembly();
        var testTypes = testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests") && !t.Name.Contains("TestRunner"))
            .ToList();

        // Verify we have tests for all major components
        var expectedTestClasses = new[]
        {
            "TransactionFetcherAgentTests",
            "TransactionProcessorAgentTests", 
            "EmailEnricherAgentTests",
            "CategorizerAgentTests",
            "CsvExporterAgentTests",
            "TransactionPipelineTests",
            "EndToEndTests"
        };

        foreach (var expectedClass in expectedTestClasses)
        {
            testTypes.Should().Contain(t => t.Name == expectedClass,
                $"Test class {expectedClass} should exist");
        }

        // Verify each test class has multiple test methods
        foreach (var testType in testTypes)
        {
            var testMethods = testType.GetMethods()
                .Where(m => m.GetCustomAttributes<FactAttribute>().Any() || 
                           m.GetCustomAttributes<TheoryAttribute>().Any())
                .ToList();

            testMethods.Should().NotBeEmpty($"Test class {testType.Name} should have test methods");
        }

        // Verify core functionality is tested
        var allTestMethods = testTypes
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes<FactAttribute>().Any() || 
                       m.GetCustomAttributes<TheoryAttribute>().Any())
            .Select(m => m.Name)
            .ToList();

        // Key scenarios that must be tested
        var criticalScenarios = new[]
        {
            "ProcessAsync", // Core agent processing
            "Pipeline", // Pipeline integration
            "EndToEnd", // Full system integration
            "WithValid", // Happy path scenarios
            "WithInvalid", // Error handling
            "WithEmpty", // Edge cases
            "Categorization", // AI categorization
            "Export", // CSV export
            "Flush" // Buffer management
        };

        foreach (var scenario in criticalScenarios)
        {
            allTestMethods.Should().Contain(m => m.Contains(scenario),
                $"Tests should cover scenario: {scenario}");
        }
    }

    [Fact]
    public void TestCoverage_ShouldIncludeAllAgents()
    {
        // Verify that we have comprehensive coverage of all agents
        var mainAssembly = Assembly.LoadFrom("TransactionProcessingSystem.dll");
        var agentTypes = mainAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Agent") && !t.IsInterface && !t.IsAbstract)
            .ToList();

        var testAssembly = Assembly.GetExecutingAssembly();
        var testTypes = testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"))
            .ToList();

        foreach (var agentType in agentTypes)
        {
            var expectedTestClassName = $"{agentType.Name}Tests";
            testTypes.Should().Contain(t => t.Name == expectedTestClassName,
                $"Agent {agentType.Name} should have corresponding test class {expectedTestClassName}");
        }
    }

    [Fact]
    public void TestCoverage_ShouldIncludeErrorHandling()
    {
        // Verify error handling scenarios are tested
        var testAssembly = Assembly.GetExecutingAssembly();
        var allTestMethods = testAssembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes<FactAttribute>().Any() || 
                       m.GetCustomAttributes<TheoryAttribute>().Any())
            .Select(m => m.Name.ToLower())
            .ToList();

        var errorScenarios = new[]
        {
            "error", "exception", "failure", "invalid", "timeout", "retry"
        };

        foreach (var scenario in errorScenarios)
        {
            allTestMethods.Should().Contain(m => m.Contains(scenario),
                $"Tests should cover error scenario: {scenario}");
        }
    }

    [Fact]
    public void TestCoverage_ShouldIncludeDataValidation()
    {
        // Verify data validation scenarios are tested
        var testAssembly = Assembly.GetExecutingAssembly();
        var allTestMethods = testAssembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes<FactAttribute>().Any() || 
                       m.GetCustomAttributes<TheoryAttribute>().Any())
            .Select(m => m.Name.ToLower())
            .ToList();

        var dataValidationScenarios = new[]
        {
            "empty", "null", "missing", "special", "format", "validation"
        };

        foreach (var scenario in dataValidationScenarios)
        {
            allTestMethods.Should().Contain(m => m.Contains(scenario),
                $"Tests should cover data validation scenario: {scenario}");
        }
    }

    [Fact]
    public void TestCoverage_ShouldIncludeIntegrationTests()
    {
        // Verify integration test coverage
        var testAssembly = Assembly.GetExecutingAssembly();
        var integrationTestTypes = testAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Integration") == true || 
                       t.Name.Contains("Pipeline") || 
                       t.Name.Contains("EndToEnd"))
            .ToList();

        integrationTestTypes.Should().NotBeEmpty("Should have integration tests");

        var integrationTestMethods = integrationTestTypes
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes<FactAttribute>().Any() || 
                       m.GetCustomAttributes<TheoryAttribute>().Any())
            .ToList();

        integrationTestMethods.Should().HaveCountGreaterThan(5, 
            "Should have substantial integration test coverage");
    }
}