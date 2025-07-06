# Official OpenAI .NET Client JSON Schema Implementation

## Overview

The Categorizer component has been completely rewritten to use the official OpenAI .NET client with **strict JSON Schema enforcement**. This implementation uses `ChatResponseFormat.CreateJsonSchemaFormat()` to guarantee structured responses and includes robust parsing with multiple fallback mechanisms.

## Implementation Details

### Core Features

#### 1. Structured JSON Response Format
```json
{
  "category": "Food & Dining",
  "confidence": 0.95,
  "reasoning": "Transaction at Starbucks clearly indicates food and beverage purchase"
}
```

#### 2. Enhanced System Prompt
The system prompt has been redesigned to:
- Request JSON-formatted responses
- Specify required fields (category, confidence, reasoning)
- Include clear instructions for categorization
- Remove examples to allow the model to focus on structured output

#### 3. Configuration-Driven Approach
```json
{
  "OpenAI": {
    "Model": "gpt-4-turbo",
    "UseJsonSchema": true,
    "Temperature": 0.1,
    "MaxTokens": 200
  }
}
```

## Technical Implementation

### Model Selection
- **Primary Model**: GPT-4o-mini (JSON Schema support required)
- **Temperature**: 0.1 for consistent, precise responses
- **SDK**: Official OpenAI .NET client v2.0.0

### Official OpenAI Client Implementation

#### 1. Client Initialization
```csharp
private readonly OpenAIClient _openAIClient;

public Categorizer(OpenAISettings settings, ...)
{
    _openAIClient = new OpenAIClient(_settings.ApiKey);
}
```

#### 2. JSON Schema Definition
```csharp
private static readonly string CategoryJsonSchema = """
{
    "type": "object",
    "properties": {
        "category": {
            "type": "string",
            "enum": ["Food & Dining", "Transportation", ...]
        },
        "confidence": {
            "type": "number",
            "minimum": 0.0,
            "maximum": 1.0
        },
        "reasoning": {
            "type": "string",
            "maxLength": 200
        }
    },
    "required": ["category", "confidence", "reasoning"],
    "additionalProperties": false
}
""";
```

#### 3. Structured Response Enforcement
```csharp
var options = new ChatCompletionOptions
{
    Temperature = (float)_settings.Temperature,
    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        jsonSchemaFormatName: "transaction_categorization",
        jsonSchema: BinaryData.FromString(CategoryJsonSchema),
        jsonSchemaIsStrict: true)
};

var completion = await _openAIClient
    .GetChatClient(model)
    .CompleteChatAsync(messages, options);
```

#### 4. Response Processing Pipeline
1. **Primary**: JsonDocument parsing with property validation
2. **Secondary**: Text extraction from malformed JSON
3. **Tertiary**: Rule-based categorization using keyword matching

#### 5. JsonDocument Response Parsing
```csharp
private string ParseStructuredJsonResponse(string transactionId, string jsonContent)
{
    using JsonDocument structuredJson = JsonDocument.Parse(jsonContent);
    
    var category = structuredJson.RootElement.GetProperty("category").GetString();
    var confidence = structuredJson.RootElement.GetProperty("confidence").GetDouble();
    var reasoning = structuredJson.RootElement.GetProperty("reasoning").GetString();

    _logger.LogDebug("OpenAI categorization for transaction {Id}: {Category} (confidence: {Confidence:P1}, reasoning: {Reasoning})", 
        transactionId, category, confidence, reasoning);

    return ValidateCategory(category);
}
```

#### 6. Category Validation
- Validates against predefined category list
- Supports exact and partial matching
- Defaults to "Other" for invalid categories

### Error Handling

#### JSON Parsing Errors
- Logs malformed responses for debugging
- Attempts text extraction as fallback
- Maintains processing continuity

#### API Failures
- Comprehensive error logging with context
- Automatic fallback to rule-based categorization
- Preserves transaction processing flow

## Benefits

### 1. Guaranteed Response Structure
- **Strict JSON Schema**: `jsonSchemaIsStrict: true` enforces exact format compliance
- **Type Safety**: Properties are guaranteed to exist and have correct types
- **Validation**: Schema validation happens at the OpenAI API level

### 2. Enhanced Reliability
- **Official SDK**: Uses OpenAI's maintained and supported .NET client
- **Error Reduction**: Malformed responses are virtually eliminated
- **Consistent Parsing**: JsonDocument provides robust property access

### 3. Production-Grade Quality
- **Real-time Validation**: Schema violations are caught immediately
- **Transparency**: Confidence scores and reasoning for every decision
- **Comprehensive Logging**: Full categorization context for debugging

### 4. Performance Benefits
- **Reduced Retries**: Schema enforcement minimizes parsing failures
- **Efficient Processing**: JsonDocument parsing is optimized
- **Minimal Fallbacks**: Structured responses reduce fallback usage

## Configuration Options

### OpenAI Settings
- `UseJsonSchema`: Enable/disable JSON response formatting
- `Model`: Specify GPT model (gpt-4-turbo, gpt-3.5-turbo, etc.)
- `Temperature`: Control response consistency (0.1 recommended)
- `MaxTokens`: Limit response length (200 recommended)

### Model Compatibility
JSON Schema enforcement requires specific OpenAI models:
- **GPT-4o-mini** (primary choice - cost-effective with schema support)
- **GPT-4o** (enhanced capabilities)
- **GPT-4-turbo** (if available)

**Note**: Older models like GPT-3.5-turbo do not support structured output with JSON Schema.

## Example Responses

### Successful JSON Response
```json
{
  "category": "Transportation",
  "confidence": 0.88,
  "reasoning": "Shell gas station indicates fuel purchase for vehicle transportation"
}
```

### Logging Output
```
[Debug] OpenAI categorization for transaction txn_123: Transportation (confidence: 88.0%, reasoning: Shell gas station indicates fuel purchase for vehicle transportation)
```

## Future Enhancements

### Formal JSON Schema Support
When the OpenAI .NET SDK supports formal JSON Schema enforcement:
- Schema validation at API level
- Guaranteed response structure
- Reduced parsing errors

### Response Schema Definition
```json
{
  "type": "object",
  "properties": {
    "category": {
      "type": "string",
      "enum": ["Food & Dining", "Transportation", ...]
    },
    "confidence": {
      "type": "number",
      "minimum": 0.0,
      "maximum": 1.0
    },
    "reasoning": {
      "type": "string",
      "maxLength": 200
    }
  },
  "required": ["category", "confidence", "reasoning"]
}
```

## Performance Considerations

### Token Optimization
- Structured prompts reduce unnecessary tokens
- Lower temperature ensures focused responses
- 200-token limit balances completeness with efficiency

### Cost Management
- GPT-4-turbo provides optimal price/performance ratio
- Structured responses reduce retry scenarios
- Fallback mechanisms minimize API dependency

## Monitoring and Analytics

### Key Metrics
- JSON parsing success rate
- Confidence score distribution
- Fallback activation frequency
- Category distribution accuracy

### Troubleshooting
- Monitor for consistent JSON parsing failures
- Track confidence scores below 0.7 for manual review
- Analyze fallback usage patterns for optimization opportunities

This implementation represents a production-ready approach to structured AI responses while maintaining backward compatibility and robust error handling.