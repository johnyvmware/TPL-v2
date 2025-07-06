# Transaction Processing System

[![CI - Build, Test & Coverage](https://github.com/your-username/transaction-processing-system/actions/workflows/ci.yml/badge.svg)](https://github.com/your-username/transaction-processing-system/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/your-username/transaction-processing-system/branch/main/graph/badge.svg)](https://codecov.io/gh/your-username/transaction-processing-system)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

> **Production-Ready Transaction Processing System** with streamlined CI/CD pipeline focusing on build quality, comprehensive testing, and code coverage.

## 🚀 CI/CD Status

Our streamlined CI/CD pipeline ensures code quality through:

- **🔨 Build**: Automated compilation and dependency resolution
- **🧪 Testing**: Comprehensive unit and integration test execution
- **📊 Coverage**: Detailed code coverage analysis and reporting
- **✅ Quality**: Automated quality gates and reporting

## 🏗️ Architecture

5-stage processing pipeline using TPL Dataflow:
```
TransactionFetcher → TransactionProcessor → EmailEnricher → Categorizer → CsvExporter
```

## 📦 Components

### TransactionFetcher
- HTTP REST API client with retry logic (3 attempts, exponential backoff)
- JSON deserialization with data validation
- Configurable timeout (30s default)

### TransactionProcessor
- Text normalization using regex patterns
- Date/amount formatting
- Title case conversion

### EmailEnricher
- Microsoft Graph SDK integration using Azure Identity
- Email search within ±2 days of transaction date
- Amount correlation and keyword matching

### Categorizer
- OpenAI ChatGPT integration with JSON Schema enforcement
- **Structured Output**: Uses `ChatResponseFormat.CreateJsonSchemaFormat()` with strict validation
- **Response Format**: 
  ```json
  {
    "category": "Food & Dining",
    "confidence": 0.95,
    "reasoning": "Transaction description indicates restaurant purchase"
  }
  ```
- **Categories**: Food & Dining, Transportation, Shopping, Utilities, Entertainment, Healthcare, Education, Travel, Financial Services, Business Services, Other
- **Model**: GPT-4o-mini with JSON Schema support
- **Fallback**: Rule-based categorization using keyword matching

### CsvExporter
- Buffered writing (100 transactions default)
- Thread-safe concurrent processing
- Automatic flush every 30 seconds

## 🔧 Configuration

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4o-mini",
    "MaxTokens": 200,
    "Temperature": 0.1
  },
  "MicrosoftGraph": {
    "ClientId": "your-azure-app-client-id",
    "ClientSecret": "your-azure-app-client-secret",
    "TenantId": "your-azure-tenant-id",
    "EmailSearchDays": 2
  },
  "TransactionApi": {
    "BaseUrl": "https://api.yourbank.com",
    "MaxRetries": 3,
    "TimeoutSeconds": 30
  },
  "Export": {
    "OutputDirectory": "./output",
    "BufferSize": 100
  },
  "Pipeline": {
    "BoundedCapacity": 100,
    "MaxDegreeOfParallelism": 4,
    "TimeoutMinutes": 10
  }
}
```

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Azure App Registration (Microsoft Graph access)
- OpenAI API key
- Internet connection

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/transaction-processing-system.git
   cd transaction-processing-system
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the application**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run tests with coverage**
   ```bash
   dotnet test --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/TransactionProcessingSystem
   ```

## 📊 Code Coverage

The project maintains high code coverage standards:

- **Target Coverage**: 80%+ line coverage
- **Coverage Reports**: Generated automatically on each CI run
- **Coverage Tools**: Coverlet for collection, ReportGenerator for HTML reports
- **Integration**: Codecov for coverage tracking and PR comments

### Viewing Coverage Reports

After running tests, coverage reports are available in:
- **HTML Report**: `TestResults/Coverage/index.html`
- **Cobertura XML**: `TestResults/**/coverage.cobertura.xml`
- **Codecov Dashboard**: View trends and detailed coverage analysis

## 🔍 Testing Strategy

Our comprehensive testing approach includes:

- **Unit Tests**: Individual component testing with mocks
- **Integration Tests**: End-to-end pipeline testing
- **Test Categories**: Organized by component and functionality
- **Coverage Analysis**: Line and branch coverage reporting
- **Continuous Testing**: Automated test execution on every change

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test category
dotnet test --filter Category=Unit
```

## 🏭 Build & Quality

### Build Process

The streamlined build process ensures:

- **Fast Builds**: Optimized dependency caching
- **Reproducible Builds**: Consistent build environments
- **Quality Gates**: Automated code quality checks
- **Artifact Management**: Build outputs properly versioned

### Quality Assurance

- ✅ **Automated Testing**: Comprehensive test suite execution
- ✅ **Code Coverage**: Detailed coverage analysis and reporting
- ✅ **Build Validation**: Compilation and dependency verification
- ✅ **Consistent CI**: Same build process for all environments

## 📈 Performance

- **Throughput**: Processes 1000+ transactions/minute
- **Memory Usage**: Optimized buffering with configurable memory footprint
- **Scalability**: Horizontal scaling support through configuration
- **Fault Tolerance**: Robust error handling and retry mechanisms

## 📋 Output

CSV files with format: `transactions_yyyyMMdd_HHmmss.csv`

**Columns**: Id, Date, Amount, Description, CleanDescription, EmailSubject, EmailSnippet, Category, Status

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the coding standards
4. Add tests for new functionality
5. Ensure all tests pass and coverage is maintained (`dotnet test`)
6. Commit your changes (`git commit -m 'feat: add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Development Commands

```bash
# Setup and restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests with coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:TestResults/Coverage -reporttypes:HtmlInline_AzurePipelines
```

## 🔧 Development Tools

- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider
- **Package Management**: NuGet with dependency restoration
- **Testing Framework**: xUnit with FluentAssertions
- **Coverage Tools**: Coverlet + ReportGenerator
- **Build Tools**: .NET CLI with MSBuild

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📖 **Documentation**: [Wiki](https://github.com/your-username/transaction-processing-system/wiki)
- 🐛 **Bug Reports**: [Issues](https://github.com/your-username/transaction-processing-system/issues)
- 💡 **Feature Requests**: [Discussions](https://github.com/your-username/transaction-processing-system/discussions)
- 📧 **Contact**: [email@example.com](mailto:email@example.com)

---

*Built with ❤️ focusing on quality, testing, and maintainability*