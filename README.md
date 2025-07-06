# TPL Dataflow Transaction Processing System

[![CI/CD Pipeline](https://github.com/your-username/transaction-processing-system/actions/workflows/ci.yml/badge.svg)](https://github.com/your-username/transaction-processing-system/actions/workflows/ci.yml)
[![PR Validation](https://github.com/your-username/transaction-processing-system/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/your-username/transaction-processing-system/actions/workflows/pr-validation.yml)
[![Release & Deploy](https://github.com/your-username/transaction-processing-system/actions/workflows/release.yml/badge.svg)](https://github.com/your-username/transaction-processing-system/actions/workflows/release.yml)
[![codecov](https://codecov.io/gh/your-username/transaction-processing-system/branch/main/graph/badge.svg)](https://codecov.io/gh/your-username/transaction-processing-system)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

> **Production-Ready Transaction Processing System** with comprehensive CI/CD pipeline, automated testing, security scanning, and multi-platform deployments.

## 🚀 CI/CD Status

| Workflow | Status | Description |
|----------|--------|-------------|
| **CI/CD Pipeline** | ![CI Status](https://github.com/your-username/transaction-processing-system/actions/workflows/ci.yml/badge.svg) | Continuous integration with build, test, and quality checks |
| **PR Validation** | ![PR Status](https://github.com/your-username/transaction-processing-system/actions/workflows/pr-validation.yml/badge.svg) | Pull request validation and code quality enforcement |
| **Release & Deploy** | ![Release Status](https://github.com/your-username/transaction-processing-system/actions/workflows/release.yml/badge.svg) | Automated releases and multi-environment deployments |
| **Dependency Updates** | ![Dependabot Status](https://img.shields.io/badge/dependabot-enabled-brightgreen.svg) | Automated dependency updates and security patches |

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

4. **Run tests**
   ```bash
   dotnet test --configuration Release
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/TransactionProcessingSystem
   ```

## 🏭 Production Deployment

### Automated Deployments

The system includes a comprehensive CI/CD pipeline with:

- **Continuous Integration**: Automated build, test, and quality checks on every commit
- **Security Scanning**: Vulnerability detection and dependency auditing
- **Multi-Platform Builds**: Linux, Windows, and macOS artifacts
- **Staged Deployments**: Automatic staging deployments for testing
- **Production Releases**: Protected production deployments with approval gates

### Environment Configuration

| Environment | Branch | Deployment Trigger | URL |
|-------------|--------|-------------------|-----|
| **Development** | `feature/*` | Manual | Local development |
| **Staging** | `develop` | Automatic on push | `https://staging.example.com` |
| **Production** | `main` | Release tags | `https://app.example.com` |

### Release Process

1. **Create a release tag**:
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

2. **Automated pipeline**:
   - Validates the release
   - Builds multi-platform artifacts
   - Creates GitHub release with changelog
   - Deploys to staging
   - Awaits production approval
   - Deploys to production

## 🔒 Security & Quality

### Security Features

- ✅ **Vulnerability Scanning**: Automated dependency vulnerability checks
- ✅ **Code Analysis**: Static code analysis with .NET analyzers
- ✅ **Dependency Review**: Automated review of new dependencies in PRs
- ✅ **Secret Management**: Environment-based configuration for sensitive data
- ✅ **HTTPS Enforcement**: Secure communication channels

### Quality Assurance

- ✅ **Unit Tests**: Comprehensive test coverage
- ✅ **Integration Tests**: End-to-end testing
- ✅ **Code Formatting**: Enforced code style with `dotnet format`
- ✅ **Conventional Commits**: Structured commit message validation
- ✅ **Pull Request Reviews**: Required code reviews before merging

## 📊 Monitoring & Observability

### Built-in Logging

The application includes structured logging with:

- **Console Logging**: Development and container environments
- **File Logging**: Production file-based logging
- **Structured Logs**: JSON formatted logs for easy parsing
- **Performance Metrics**: Transaction processing times and throughput

### Health Checks

Production deployments include:

- **Startup Validation**: Application startup health verification
- **Dependency Checks**: External service connectivity validation
- **Performance Monitoring**: Response time and resource usage tracking

## 📋 Output

CSV files with format: `transactions_yyyyMMdd_HHmmss.csv`

**Columns**: Id, Date, Amount, Description, CleanDescription, EmailSubject, EmailSnippet, Category, Status

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the coding standards
4. Add tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Commit using conventional commits (`git commit -m 'feat: add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Development Workflow

```bash
# Setup development environment
make setup-dev

# Run tests with coverage
make test-coverage

# Format code
make format

# Run security scan
make security-scan

# Build for production
make build-release
```

## 📈 Performance

- **Throughput**: Processes 1000+ transactions/minute
- **Memory Usage**: Optimized buffering with configurable memory footprint
- **Scalability**: Horizontal scaling support through configuration
- **Fault Tolerance**: Robust error handling and retry mechanisms

## 🔧 Development Tools

- **IDE**: Visual Studio 2022, VS Code, or JetBrains Rider
- **Package Management**: NuGet with automated dependency updates
- **Testing**: xUnit with FluentAssertions for readable tests
- **Debugging**: Comprehensive logging and error reporting

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📖 **Documentation**: [Wiki](https://github.com/your-username/transaction-processing-system/wiki)
- 🐛 **Bug Reports**: [Issues](https://github.com/your-username/transaction-processing-system/issues)
- 💡 **Feature Requests**: [Discussions](https://github.com/your-username/transaction-processing-system/discussions)
- 📧 **Contact**: [email@example.com](mailto:email@example.com)

---

*Made with ❤️ by the Transaction Processing Team*