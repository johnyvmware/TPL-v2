# TPL Dataflow Transaction Processing System

## Architecture

5-stage processing pipeline using TPL Dataflow:
```
TransactionFetcher → TransactionProcessor → EmailEnricher → Categorizer → CsvExporter
```

## Components

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

## Configuration

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

## Usage

```bash
dotnet run --project src/TransactionProcessingSystem
```

## Requirements

- .NET 8.0
- Azure App Registration (Microsoft Graph access)
- OpenAI API key
- Internet connection

## Output

CSV files with format: `transactions_yyyyMMdd_HHmmss.csv`

Columns: Id, Date, Amount, Description, CleanDescription, EmailSubject, EmailSnippet, Category, Status