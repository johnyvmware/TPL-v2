using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class Categorizer : ProcessorBase<Transaction, Transaction>
{
    private readonly OpenAIClient _openAIClient;
    private readonly OpenAISettings _settings;

    private static readonly string SystemPrompt = """
        You are a financial transaction categorizer. Analyze transaction descriptions and categorize them into one of the predefined categories.
        
        Available categories:
        - Food & Dining
        - Transportation
        - Shopping
        - Utilities
        - Entertainment
        - Healthcare
        - Education
        - Travel
        - Financial Services
        - Business Services
        - Other
        
        Rules:
        1. Choose the most appropriate category from the list above
        2. If uncertain, choose the most likely category
        3. Use "Other" only if no other category fits
        4. Consider both the description and any email context provided
        5. Provide a confidence score between 0.0 and 1.0
        6. Include a brief reasoning for your choice
        
        You must respond with valid JSON matching the required schema.
        """;

    private static readonly string CategoryJsonSchema = """
        {
            "type": "object",
            "properties": {
                "category": {
                    "type": "string",
                    "enum": [
                        "Food & Dining",
                        "Transportation", 
                        "Shopping",
                        "Utilities",
                        "Entertainment",
                        "Healthcare",
                        "Education",
                        "Travel",
                        "Financial Services",
                        "Business Services",
                        "Other"
                    ],
                    "description": "The most appropriate category for this transaction"
                },
                "confidence": {
                    "type": "number",
                    "minimum": 0.0,
                    "maximum": 1.0,
                    "description": "Confidence score for the categorization (0.0 to 1.0)"
                },
                "reasoning": {
                    "type": "string",
                    "maxLength": 200,
                    "description": "Brief explanation for the categorization choice"
                }
            },
            "required": ["category", "confidence", "reasoning"],
            "additionalProperties": false
        }
        """;

    public Categorizer(
        IOptions<OpenAISettings> settings,
        IOptions<OpenAISecrets> secrets,
        ILogger<Categorizer> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        // With .NET 9's .ValidateOnStart(), settings.Value and secrets.Value are guaranteed to be valid
        _settings = settings.Value;
        _openAIClient = new OpenAIClient(secrets.Value.ApiKey);
    }

    protected override async Task<Transaction> ProcessAsync(Transaction transaction)
    {
        _logger.LogDebug("Categorizing transaction {Id}: {Description}",
            transaction.Id, transaction.CleanDescription ?? transaction.Description);

        try
        {
            var category = await CategorizeWithOpenAI(transaction);

            var categorizedTransaction = transaction with
            {
                Category = category,
                Status = ProcessingStatus.Categorized
            };

            _logger.LogDebug("Categorized transaction {Id} as: {Category}",
                transaction.Id, category);

            return categorizedTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to categorize transaction {Id} with OpenAI, using fallback", transaction.Id);

            // Fallback to rule-based categorization only on error
            var fallbackCategory = GetFallbackCategory(transaction);
            return transaction with
            {
                Category = fallbackCategory,
                Status = ProcessingStatus.Categorized
            };
        }
    }

    private async Task<string> CategorizeWithOpenAI(Transaction transaction)
    {
        var userMessage = BuildUserMessage(transaction);

        List<ChatMessage> messages = [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(userMessage)
        ];

        var options = new ChatCompletionOptions
        {
            Temperature = (float)_settings.Temperature
        };

        // Always use JSON Schema enforcement for structured output
        options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "transaction_categorization",
            jsonSchema: BinaryData.FromString(CategoryJsonSchema),
            jsonSchemaIsStrict: true);

        var model = !string.IsNullOrEmpty(_settings.Model) ? _settings.Model : "gpt-4o-mini";
        var completion = await _openAIClient.GetChatClient(model).CompleteChatAsync(messages, options);

        if (completion?.Value?.Content == null || completion.Value.Content.Count == 0)
        {
            _logger.LogWarning("Invalid response from OpenAI for transaction {Id}", transaction.Id);
            throw new InvalidOperationException("OpenAI API returned empty response");
        }

        var content = completion.Value.Content[0].Text;

        // Parse structured JSON response
        return ParseStructuredJsonResponse(transaction.Id, content);
    }

    private bool IsJsonSchemaSupported(string model)
    {
        // JSON output is supported by GPT-4 and newer models via prompt engineering
        // Future SDK versions will support formal JSON Schema enforcement
        var jsonCapableModels = new[]
        {
            "gpt-4",
            "gpt-4-turbo",
            "gpt-4-turbo-preview",
            "gpt-3.5-turbo"
        };

        return jsonCapableModels.Any(supportedModel =>
            model.StartsWith(supportedModel, StringComparison.OrdinalIgnoreCase));
    }

    private string ParseStructuredJsonResponse(string transactionId, string jsonContent)
    {
        try
        {
            using JsonDocument structuredJson = JsonDocument.Parse(jsonContent);

            var category = structuredJson.RootElement.GetProperty("category").GetString();
            var confidence = structuredJson.RootElement.GetProperty("confidence").GetDouble();
            var reasoning = structuredJson.RootElement.GetProperty("reasoning").GetString();

            if (string.IsNullOrEmpty(category))
            {
                throw new InvalidOperationException("Category field is missing or empty in JSON response");
            }

            _logger.LogDebug("OpenAI categorization for transaction {Id}: {Category} (confidence: {Confidence:P1}, reasoning: {Reasoning})",
                transactionId, category, confidence, reasoning);

            return ValidateCategory(category);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse structured JSON response from OpenAI for transaction {Id}. Response: {Response}",
                transactionId, jsonContent);

            // Try to extract category from malformed JSON as fallback
            return ExtractCategoryFromText(jsonContent);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Required property missing in JSON response for transaction {Id}. Response: {Response}",
                transactionId, jsonContent);

            return ExtractCategoryFromText(jsonContent);
        }
    }

    private string BuildUserMessage(Transaction transaction)
    {
        var message = $"Amount: ${transaction.Amount:F2}\n";
        message += $"Description: {transaction.CleanDescription ?? transaction.Description}\n";
        message += $"Date: {transaction.Date:yyyy-MM-dd}";

        if (!string.IsNullOrEmpty(transaction.EmailSubject))
        {
            message += $"\nEmail Subject: {transaction.EmailSubject}";
        }

        if (!string.IsNullOrEmpty(transaction.EmailSnippet))
        {
            message += $"\nEmail Snippet: {transaction.EmailSnippet}";
        }

        return message;
    }

    private string ValidateCategory(string category)
    {
        var validCategories = new[]
        {
            "Food & Dining",
            "Transportation",
            "Shopping",
            "Utilities",
            "Entertainment",
            "Healthcare",
            "Education",
            "Travel",
            "Financial Services",
            "Business Services",
            "Other"
        };

        // Find exact match
        var exactMatch = validCategories.FirstOrDefault(c =>
            string.Equals(c, category, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
            return exactMatch;

        // Find partial match
        var partialMatch = validCategories.FirstOrDefault(c =>
            c.Contains(category, StringComparison.OrdinalIgnoreCase) ||
            category.Contains(c, StringComparison.OrdinalIgnoreCase));

        if (partialMatch != null)
            return partialMatch;

        _logger.LogWarning("Invalid category '{Category}' returned from OpenAI, using 'Other'", category);
        return "Other";
    }

    private string GetFallbackCategory(Transaction transaction)
    {
        var description = (transaction.CleanDescription ?? transaction.Description).ToLower();

        var categoryKeywords = new Dictionary<string, string[]>
        {
            ["Food & Dining"] = ["restaurant", "food", "cafe", "pizza", "burger", "starbucks", "mcdonald", "dining"],
            ["Transportation"] = ["gas", "fuel", "uber", "lyft", "taxi", "bus", "train", "parking", "shell", "chevron"],
            ["Shopping"] = ["amazon", "walmart", "target", "store", "market", "shop", "retail", "purchase"],
            ["Utilities"] = ["electric", "gas", "water", "internet", "phone", "utility", "power", "cable"],
            ["Entertainment"] = ["netflix", "spotify", "movie", "theater", "game", "entertainment", "music"],
            ["Healthcare"] = ["pharmacy", "hospital", "doctor", "medical", "health", "clinic", "cvs", "walgreens"],
            ["Financial Services"] = ["bank", "atm", "fee", "interest", "transfer", "payment", "credit", "loan"],
            ["Travel"] = ["hotel", "airline", "flight", "booking", "travel", "vacation", "trip"],
            ["Business Services"] = ["office", "supplies", "service", "professional", "consulting", "software"]
        };

        foreach (var (category, keywords) in categoryKeywords)
        {
            if (keywords.Any(keyword => description.Contains(keyword)))
            {
                return category;
            }
        }

        return "Other";
    }

    private string ExtractCategoryFromText(string text)
    {
        var validCategories = new[]
        {
            "Food & Dining",
            "Transportation",
            "Shopping",
            "Utilities",
            "Entertainment",
            "Healthcare",
            "Education",
            "Travel",
            "Financial Services",
            "Business Services",
            "Other"
        };

        // Try to find any valid category mentioned in the text
        foreach (var category in validCategories)
        {
            if (text.Contains(category, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Extracted category '{Category}' from malformed response: {Text}", category, text);
                return category;
            }
        }

        _logger.LogWarning("Could not extract valid category from response: {Text}", text);
        return "Other";
    }
}