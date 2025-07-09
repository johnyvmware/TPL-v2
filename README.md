# Transaction Processing System

> **A next-generation transaction processing platform** featuring a high-performance TPL Dataflow pipeline, intelligent categorization with OpenAI GPT-4, advanced analytics via Neo4j graph database, seamless Microsoft Graph integration, and robust automated testing with a full CI/CD pipeline.

## 🚀 Automation & CI/CD

Our CI/CD and automation suite ensures every commit is built, tested, and delivered with maximum reliability and security.

| Component                | Status                                                                                                   | Description                                              |
|--------------------------|----------------------------------------------------------------------------------------------------------|----------------------------------------------------------|
| **Main CI/CD Pipeline**  | ![CI/CD](https://github.com/johnyvmware/TPL-v2/actions/workflows/ci.yml/badge.svg)                      | Orchestrates build, test, security, and release on `main`|
| &nbsp;&nbsp;• Build & Test      | ![Build](https://img.shields.io/badge/build-automated-blue.svg)                                         | Automated .NET build, NUnit tests, and coverage analysis |
| &nbsp;&nbsp;• Security Scan     | ![Security](https://img.shields.io/badge/security-scan-green.svg)                                      | Scans for vulnerabilities and secrets                    |
| &nbsp;&nbsp;• Release Artifacts | ![Artifacts](https://img.shields.io/badge/artifacts-generated-purple.svg)                               | Publishes self-contained application packages            |
| **PR Validation**        | ![PR Validation](https://github.com/johnyvmware/TPL-v2/actions/workflows/pr-validation.yml/badge.svg)     | Enforces quality gates for all pull requests             |
| **Code Coverage**        | ![Coverage](https://codecov.io/gh/johnyvmware/TPL-v2/branch/main/graph/badge.svg)                        | Tracks and reports test coverage metrics                 |
| **Code Formatting**      | ![Format](https://img.shields.io/badge/dotnet_format-enforced-blue.svg)                                  | Enforces consistent code style automatically             |
| **Dependency Updates**   | ![Dependabot](https://img.shields.io/badge/dependabot-weekly-blue.svg)                                   | Keeps NuGet and GitHub Actions dependencies up to date   |

**Highlights:**
- **Full automation**: Every push and PR triggers the pipeline for instant feedback.
- **Security-first**: Integrated scanning for vulnerabilities and secrets.
- **Quality assurance**: Automated tests, code coverage, and formatting checks on every change.
- **Continuous delivery**: Artifacts are built and published for every release.
- **Effortless maintenance**: Dependencies are updated automatically via Dependabot.

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

---

Built with ❤️ by AI