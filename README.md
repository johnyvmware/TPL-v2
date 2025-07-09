# Transaction Processing System

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NUnit](https://img.shields.io/badge/Testing-NUnit-green.svg)](https://nunit.org/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

> **Modern Transaction Processing System** with TPL Dataflow pipeline, Neo4j graph database integration, and comprehensive testing.

## 🚀 Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Neo4j Database](https://neo4j.com/download/) (optional for full functionality)

### Build and Run
```bash
# Clone the repository
git clone https://github.com/johnyvmware/TPL-v2.git
cd TPL-v2

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/TransactionProcessingSystem
```

## 📁 Project Structure

```
TPL-v2/
├── src/
│   └── TransactionProcessingSystem/           # Main application
│       ├── Components/                        # Processing components
│       ├── Models/                           # Data models
│       ├── Services/                         # Business services
│       ├── Configuration/                    # App configuration
│       └── Pipeline/                         # TPL Dataflow pipeline
├── tests/
│   └── TransactionProcessingSystem.Tests/    # Consolidated test project
│       ├── UnitTests/                        # Unit tests
│       └── IntegrationTests/                 # Integration tests
└── TPL-v2.sln                              # Solution file
```

## 🏗️ Architecture

Modern 5-stage processing pipeline using TPL Dataflow:

```
TransactionFetcher → TransactionProcessor → EmailEnricher → Categorizer → CsvExporter
```

### Key Features
- **Asynchronous Processing**: Leverages TPL Dataflow for high-performance parallel processing
- **Graph Database**: Neo4j integration for advanced transaction analytics and relationship mapping
- **AI Categorization**: OpenAI GPT-4 integration for intelligent transaction categorization
- **Email Correlation**: Microsoft Graph API integration for transaction-email matching
- **Robust Testing**: Comprehensive test suite with NUnit framework

## 📦 Core Components

### TransactionFetcher
- HTTP REST API client with retry logic (3 attempts, exponential backoff)
- JSON deserialization with data validation
- Configurable timeout (30s default)

### TransactionProcessor
- Text normalization using regex patterns
- Date/amount formatting and validation
- Title case conversion and data cleansing

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

### Neo4jProcessor
- Graph database integration for advanced analytics
- Transaction relationship mapping
- Real-time similarity detection
- IAsyncEnumerable streaming for large datasets

## 🔧 Configuration

Create an `appsettings.json` file:

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
  "Neo4j": {
    "ConnectionUri": "neo4j://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 10,
    "ConnectionTimeoutSeconds": 30
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

## 🧪 Testing

The project uses **NUnit** as the testing framework with comprehensive test coverage:

### Test Structure
```
tests/TransactionProcessingSystem.Tests/
├── UnitTests/
│   └── BasicTests.cs                    # Core model and logic tests
├── IntegrationTests/                    # End-to-end integration tests
└── SimpleTests.cs                       # Basic functionality tests
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run tests with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

### Test Features
- ✅ **Unit Tests**: Core business logic and model validation
- ✅ **Integration Tests**: Database and external service integration
- ✅ **Mocking**: Moq framework for dependency isolation
- ✅ **Assertions**: FluentAssertions for readable test assertions
- ✅ **Async Testing**: Full support for async/await patterns

## 📋 Output

The system generates CSV files with the format: `transactions_yyyyMMdd_HHmmss.csv`

**Columns**: Id, Date, Amount, Description, CleanDescription, EmailSubject, EmailSnippet, Category, Status

## 🔒 Security & Quality

### Security Features
- ✅ **Secure Configuration**: Environment-based secrets management
- ✅ **HTTPS Enforcement**: Secure communication channels
- ✅ **Input Validation**: Comprehensive data validation
- ✅ **Error Handling**: Robust exception handling and logging

### Quality Assurance
- ✅ **Comprehensive Testing**: Unit and integration test coverage
- ✅ **Code Standards**: Consistent coding style and conventions
- ✅ **Static Analysis**: Built-in .NET analyzers
- ✅ **Modern C# Features**: Leverages latest C# language features

## 📊 Monitoring & Observability

### Built-in Logging
- **Console Logging**: Development and container environments
- **Structured Logs**: JSON formatted logs for easy parsing
- **Performance Metrics**: Transaction processing times and throughput
- **Error Tracking**: Comprehensive error reporting and stack traces

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the coding standards
4. Add tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'feat: add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## 🔧 Development Tools

- **Framework**: .NET 8.0 with latest C# features
- **Testing**: NUnit with FluentAssertions for readable tests
- **Mocking**: Moq for dependency isolation
- **Database**: Neo4j for graph-based analytics
- **Package Management**: NuGet with modern package references
- **IDE Support**: Visual Studio, VS Code, Rider compatible

## 🆘 Support

For questions, issues, or contributions:

- 🐛 **Bug Reports**: Create an issue with detailed reproduction steps
- 💡 **Feature Requests**: Open a discussion for new feature ideas
- 📖 **Documentation**: Check the inline code documentation and XML comments

---

Built with ❤️ by AI