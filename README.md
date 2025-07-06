# Agent-Based TPL Dataflow Transaction Processing System

## Overview

A production-grade financial transaction processing pipeline built with C# and TPL Dataflow architecture. The system processes transactions through a 5-stage agent pipeline, enriching data with email context using Microsoft Graph and categorizing transactions with OpenAI's ChatGPT.

## System Architecture

### Pipeline Flow
```
TransactionFetcherAgent → TransactionProcessorAgent → EmailEnricherAgent → CategorizerAgent → CsvExporterAgent
```

Each agent operates as an independent processing unit with:
- **Bounded Capacity**: Memory-safe processing with configurable limits
- **Async Processing**: Full async/await with cancellation support
- **Error Isolation**: Individual agent failures don't cascade
- **Monitoring**: Structured logging throughout all operations

## Core Components

### 1. TransactionFetcherAgent
Fetches transaction data from REST APIs with enterprise-grade reliability:
- HTTP retry logic with exponential backoff
- Configurable timeout handling (30s default)
- JSON deserialization with validation
- Data type conversion with fallback values

### 2. TransactionProcessorAgent
Cleans and normalizes transaction data:
- Advanced regex-based text processing
- Intelligent abbreviation handling (ATM, LLC, INC, etc.)
- Redundant term removal
- Date/time normalization
- Amount formatting with precision control

### 3. EmailEnricherAgent
Enriches transactions with related email data using Microsoft Graph:
- **Authentication**: Azure Identity with client credentials
- **Email Search**: Date-range filtering (±2 days configurable)
- **Smart Matching**: Scoring algorithm based on:
  - Amount correlation using regex matching
  - Keyword extraction and matching
  - Temporal proximity to transaction date
  - Financial content detection

### 4. CategorizerAgent
Categorizes transactions using OpenAI's ChatGPT:
- **AI Integration**: Real ChatGPT API with prompt engineering
- **Categories**: Food & Dining, Transportation, Shopping, Utilities, Entertainment, Healthcare, Education, Travel, Financial Services, Business Services, Other
- **Fallback Logic**: Rule-based categorization when AI fails
- **Validation**: Response validation with category normalization

### 5. CsvExporterAgent
Exports processed transactions to CSV files:
- **Thread-Safe**: Concurrent processing with semaphore protection
- **Buffering**: Configurable buffer size with automatic flush
- **Periodic Flush**: Timer-based flush every 30 seconds
- **Proper Escaping**: CSV formatting using CsvHelper library

## Usage

### Configuration

Configure the system through `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 150,
    "Temperature": 0.3
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
    "BufferSize": 100,
    "FlushIntervalSeconds": 30
  },
  "Pipeline": {
    "BoundedCapacity": 100,
    "MaxDegreeOfParallelism": 4
  }
}
```

### Running the System

```bash
# Build the application
dotnet build

# Run with configuration
dotnet run --project src/TransactionProcessingSystem
```

The system will:
1. Start the mock transaction API service
2. Fetch transactions from the configured endpoint
3. Process them through the 5-stage pipeline
4. Export results to CSV files in the output directory

### Data Flow

#### Input Transaction
```json
{
  "id": "txn_123",
  "date": "2024-01-15T10:30:00Z",
  "amount": "45.67",
  "description": "STARBUCKS STORE #1234 SEATTLE WA"
}
```

#### Processed Output
```csv
Id,Date,Amount,Description,CleanDescription,EmailSubject,EmailSnippet,Category,Status
txn_123,2024-01-15,45.67,STARBUCKS STORE #1234 SEATTLE WA,Starbucks Store Seattle,Your Starbucks Receipt,Thank you for your purchase at Starbucks...,Food & Dining,Exported
```

## Technical Features

### Modern C# Patterns
- **Records**: Immutable data structures with required properties
- **Pattern Matching**: With expressions for data transformation
- **Nullable Reference Types**: Explicit null handling
- **Async/Await**: Non-blocking operations throughout
- **Dependency Injection**: Full DI container integration

### Production Reliability
- **Retry Policies**: Exponential backoff with jitter
- **Circuit Breakers**: Prevent cascade failures
- **Graceful Degradation**: Fallback mechanisms when external services fail
- **Bounded Queues**: Memory protection with backpressure
- **Timeout Handling**: Configurable per-operation timeouts

### Performance Characteristics
- **Configurable Parallelism**: 1-16 concurrent workers per agent
- **Bounded Capacity**: 100-item queues prevent memory bloat
- **Streaming Processing**: Memory-efficient for large datasets
- **Connection Pooling**: HTTP client reuse with dependency injection

### Monitoring & Observability
- **Structured Logging**: Contextual information throughout pipeline
- **Performance Metrics**: Processing times and throughput tracking
- **Error Tracking**: Detailed error information with correlation IDs
- **Health Checks**: Component status monitoring

## API Integrations

### Microsoft Graph
- **Authentication**: Service-to-service authentication with Azure Identity
- **Email Access**: Read emails from user's mailbox
- **Filtering**: Date range and content-based filtering
- **Rate Limiting**: Respects Graph API throttling limits

### OpenAI ChatGPT
- **Model**: GPT-3.5-turbo for transaction categorization
- **Prompt Engineering**: Optimized system prompts with examples
- **Token Management**: Configurable token limits
- **Error Handling**: Fallback categorization on API failures

## Output Files

The system generates timestamped CSV files in the configured output directory:
- **Format**: `transactions_yyyyMMdd_HHmmss.csv`
- **Encoding**: UTF-8 with BOM for Excel compatibility
- **Headers**: Full transaction data including enriched fields
- **Escaping**: Proper CSV escaping for special characters

## Error Handling

The system implements comprehensive error handling:
- **Agent-Level**: Individual agent failures don't stop the pipeline
- **Retry Logic**: Automatic retry with exponential backoff
- **Fallback Processing**: Continue processing when external services fail
- **Data Validation**: Input validation with detailed error messages
- **Logging**: All errors logged with context for debugging

## System Requirements

- **.NET 8.0**: Runtime and SDK
- **Azure App Registration**: For Microsoft Graph access
- **OpenAI API Key**: For transaction categorization
- **Internet Connection**: For API access
- **File System Access**: For CSV output

## Deployment

The system is designed for enterprise deployment with:
- **Configuration Management**: Environment-specific settings
- **Secret Management**: Secure API key storage
- **Container Support**: Docker-ready with minimal dependencies
- **Monitoring Integration**: Structured logging for external monitoring
- **Scalability**: Horizontal scaling support through configuration