# work in progress ⚠️
# Transaction Processing System

> **A next-generation transaction processing platform** featuring a high-performance TPL Dataflow pipeline, intelligent categorization with OpenAI GPT-4, advanced analytics via Neo4j graph database, seamless Microsoft Graph integration, and robust automated testing with a full CI/CD pipeline.

## 🚀 Automation & CI/CD

Ensure every commit is built, tested, and delivered with maximum reliability and security.

| Component                | Status                                                                                                   | Description                                              |
|--------------------------|----------------------------------------------------------------------------------------------------------|----------------------------------------------------------|
| **CI/CD**  | ![CI/CD](https://github.com/johnyvmware/TPL-v2/actions/workflows/ci.yml/badge.svg)                      | Orchestrates build, test, security, and release on `main`|
| &nbsp;&nbsp;• Build & Test      | | Automated .NET build, NUnit tests, and coverage analysis |
| &nbsp;&nbsp;• Security Scan     | | Scans for vulnerabilities and secrets                    |
| &nbsp;&nbsp;• Release Artifacts | | Publishes self-contained application packages            |           |
| **Code Coverage**        | ![Coverage](https://codecov.io/gh/johnyvmware/TPL-v2/branch/main/graph/badge.svg)                        | Tracks and reports test coverage metrics                 |
| **Code Formatting**      | ![Format](https://img.shields.io/badge/dotnet_format-enforced-blue.svg)                                  | Enforces consistent code style automatically             |
| **Dependency Updates**   | ![Dependabot](https://img.shields.io/badge/dependabot-weekly-blue.svg)                                   | Keeps NuGet and GitHub Actions dependencies up to date   |

## Required Secrets

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\94bedc6c-a871-4a2e-b4c5-98271ef751d2\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/94bedc6c-a871-4a2e-b4c5-98271ef751d2/secrets.json`

### 1. OpenAI Configuration
Used by the `Categorizer` component for AI-powered transaction categorization

| Secret Key | Description | Example |
|------------|-------------|---------|
| `Secrets:OpenAI:ApiKey` | Your OpenAI API key for GPT-4 access | `sk-...` |

### 2. Microsoft Graph Configuration
Used by the `EmailEnricher` component to fetch email data from Microsoft Graph API

| Secret Key | Description | Example |
|------------|-------------|---------|
| `Secrets:MicrosoftGraph:ClientId` | Azure App Registration Client ID | `12345678-1234-1234-1234-123456789012` |
| `Secrets:MicrosoftGraph:ClientSecret` | Azure App Registration Client Secret | `your-client-secret-here` |
| `Secrets:MicrosoftGraph:TenantId` | Azure Tenant ID | `87654321-4321-4321-4321-210987654321` |

### 3. Neo4j Database Configuration
Used by the `Neo4jExporter` component to store transaction data in the Neo4j graph database

| Secret Key | Description | Example |
|------------|-------------|---------|
| `Secrets:Neo4j:ConnectionUri` | Neo4j database connection URI | `bolt://localhost:7687` |
| `Secrets:Neo4j:Username` | Neo4j database username | `neo4j` |
| `Secrets:Neo4j:Password` | Neo4j database password | `your-neo4j-password` |

## 🏗️ Architecture

5-stage processing pipeline using TPL Dataflow:

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
