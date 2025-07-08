# Neo4j Aura DB Integration Guide

This document explains how to configure and use the Neo4j Aura DB integration in the Transaction Processing System.

## Overview

The Neo4j processor has been added to the transaction processing pipeline to store transactions in a graph database and create relationships between similar transactions. This enables advanced analytics, pattern detection, and relationship discovery.

## Features

- **Transaction Storage**: Stores transaction data as nodes in the Neo4j graph database
- **Relationship Creation**: Automatically creates relationships between similar transactions based on:
  - Similar amounts (within 10% or $10)
  - Same category
  - Similar descriptions (using text similarity)
- **Analytics**: Provides transaction analytics and graph statistics
- **Performance Optimization**: Includes automatic index creation for better query performance
- **Fault Tolerance**: Pipeline continues even if Neo4j operations fail

## Configuration

### 1. Neo4j Aura Setup

1. Create a free Neo4j Aura account at [https://aura.neo4j.io/](https://aura.neo4j.io/)
2. Create a new AuraDB Free instance
3. Download the connection details (URI, username, password)

### 2. Application Configuration

Update your `appsettings.json` file with your Neo4j Aura connection details:

```json
{
  "Neo4j": {
    "ConnectionUri": "neo4j+s://your-instance-id.databases.neo4j.io",
    "Username": "neo4j",
    "Password": "your-generated-password",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 50,
    "ConnectionTimeoutSeconds": 30,
    "MaxTransactionRetryTimeSeconds": 30,
    "EnableMetrics": true
  }
}
```

#### Configuration Options

- **ConnectionUri**: Your Neo4j Aura connection URI (must use `neo4j+s://` for secure connections)
- **Username**: Database username (usually "neo4j")
- **Password**: Your database password
- **Database**: Target database name (default: "neo4j")
- **MaxConnectionPoolSize**: Maximum number of connections in the pool (default: 50)
- **ConnectionTimeoutSeconds**: Connection timeout in seconds (default: 30)
- **MaxTransactionRetryTimeSeconds**: Maximum retry time for failed transactions (default: 30)
- **EnableMetrics**: Enable driver metrics collection (default: true)

### 3. Environment Variables (Alternative)

You can also configure using environment variables for better security:

```bash
NEO4J_CONNECTION_URI="neo4j+s://your-instance-id.databases.neo4j.io"
NEO4J_USERNAME="neo4j"
NEO4J_PASSWORD="your-generated-password"
NEO4J_DATABASE="neo4j"
```

## Architecture

### Pipeline Integration

The Neo4j processor is integrated into the transaction processing pipeline between the categorizer and CSV exporter:

```
TransactionFetcher → TransactionProcessor → EmailEnricher → Categorizer → Neo4jProcessor → CsvExporter
```

### Data Model

#### Transaction Node Structure

```cypher
(:Transaction {
  id: String,
  date: String,
  amount: Float,
  description: String,
  cleanDescription: String,
  emailSubject: String,
  emailSnippet: String,
  category: String,
  status: String,
  createdAt: DateTime,
  updatedAt: DateTime
})
```

#### Relationship Types

1. **SIMILAR_AMOUNT**: Connects transactions with similar amounts
   - Properties: `difference` (absolute amount difference)

2. **SAME_CATEGORY**: Connects transactions with identical categories

3. **SIMILAR_DESCRIPTION**: Connects transactions with similar descriptions
   - Properties: `similarity` (Jaro-Winkler similarity score)

### Performance Optimization

The system automatically creates the following indexes:

- `transaction_id_index`: On transaction ID
- `transaction_category_index`: On category
- `transaction_amount_index`: On amount
- `transaction_date_index`: On date
- `transaction_status_index`: On status

## Usage

### Basic Operation

The Neo4j processor runs automatically as part of the transaction processing pipeline. No additional configuration is required once the connection settings are in place.

### Querying the Graph

You can query the graph database directly using Cypher queries:

#### Find Similar Transactions

```cypher
MATCH (t1:Transaction)-[r:SIMILAR_AMOUNT]-(t2:Transaction)
WHERE t1.id = "your-transaction-id"
RETURN t1, r, t2
```

#### Get Transaction Categories

```cypher
MATCH (t:Transaction)
WHERE t.category IS NOT NULL
RETURN t.category, count(*) as transactionCount
ORDER BY transactionCount DESC
```

#### Find Transaction Patterns

```cypher
MATCH (t:Transaction)
WHERE t.amount > 100
AND t.category = "Food"
RETURN t.description, t.amount
ORDER BY t.amount DESC
```

### Analytics API

The `INeo4jDataAccess` service provides several analytics methods:

```csharp
// Get transaction analytics
var analytics = await neo4jDataAccess.GetTransactionAnalyticsAsync();

// Find similar transactions
var similar = await neo4jDataAccess.FindSimilarTransactionsAsync(transaction);

// Get graph statistics
var stats = await neo4jDataAccess.GetGraphStatsAsync();

// Execute custom queries
var results = await neo4jDataAccess.ExecuteQueryAsync("MATCH (n) RETURN count(n)");
```

## Security Best Practices

1. **Never commit passwords**: Use environment variables or secure configuration providers
2. **Use strong passwords**: Generate strong passwords for your Neo4j Aura instance
3. **Enable SSL**: Always use `neo4j+s://` URIs for encrypted connections
4. **Limit access**: Configure IP allowlists in Neo4j Aura if needed
5. **Regular rotation**: Rotate passwords periodically

## Troubleshooting

### Common Issues

#### Connection Failed

```
Failed to verify Neo4j connectivity
```

**Solutions:**
1. Verify your connection URI is correct
2. Check username and password
3. Ensure your IP is allowlisted in Neo4j Aura (if configured)
4. Check network connectivity

#### APOC Procedures Not Available

```
Failed to create relationships for transaction. This may be due to missing APOC procedures.
```

**Solutions:**
- This is normal for Neo4j Aura Free instances
- The system will fall back to basic relationship creation
- Consider upgrading to a paid plan for full APOC support

#### Performance Issues

**Solutions:**
1. Increase `MaxConnectionPoolSize` in configuration
2. Optimize Cypher queries
3. Ensure indexes are created (automatic on startup)
4. Consider upgrading Neo4j Aura plan for better performance

### Logging

Enable debug logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "TransactionProcessingSystem.Services.Neo4jDataAccess": "Debug",
      "TransactionProcessingSystem.Components.Neo4jProcessor": "Debug"
    }
  }
}
```

## Advanced Configuration

### Custom Relationship Logic

To customize relationship creation, modify the `CreateTransactionRelationshipsAsync` method in `Neo4jDataAccess.cs`:

```csharp
// Example: Add merchant-based relationships
const string merchantCypher = """
    MATCH (t1:Transaction {id: $transactionId})
    MATCH (t2:Transaction)
    WHERE t1 <> t2 
    AND t1.description CONTAINS 'WALMART' 
    AND t2.description CONTAINS 'WALMART'
    MERGE (t1)-[r:SAME_MERCHANT]-(t2)
    """;
```

### Custom Analytics

Extend the analytics capabilities by adding methods to the `INeo4jDataAccess` interface:

```csharp
Task<IDictionary<string, object>> GetMerchantAnalyticsAsync(CancellationToken cancellationToken = default);
Task<IEnumerable<Transaction>> GetSuspiciousTransactionsAsync(CancellationToken cancellationToken = default);
```

## Dependencies

- **Neo4j.Driver**: Official Neo4j .NET driver (v5.28.1)
- **Microsoft.Extensions.Options**: For configuration binding
- **Microsoft.Extensions.Logging**: For logging support

## Contributing

When modifying the Neo4j integration:

1. Follow the existing async patterns
2. Add appropriate error handling and logging
3. Update this documentation
4. Consider backward compatibility
5. Test with both Aura Free and paid instances