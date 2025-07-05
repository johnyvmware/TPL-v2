using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Agents;

public class CategorizerAgent : AgentBase<Transaction, Transaction>
{
    private readonly OpenAIService _openAIService;
    private readonly OpenAISettings _settings;
    
    private static readonly string SystemPrompt = """
        You are a financial transaction categorizer. Analyze transaction descriptions and categorize them into one of these categories:
        
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
        1. Return ONLY the category name, no additional text
        2. If uncertain, choose the most likely category
        3. Use "Other" only if no other category fits
        4. Consider both the description and any email context provided
        
        Examples:
        - "McDonald's Restaurant" -> Food & Dining
        - "Shell Gas Station" -> Transportation
        - "Amazon Purchase" -> Shopping
        - "Electric Bill Payment" -> Utilities
        - "Netflix Subscription" -> Entertainment
        """;

    public CategorizerAgent(
        OpenAISettings settings,
        ILogger<CategorizerAgent> logger,
        int boundedCapacity = 100) 
        : base(logger, boundedCapacity)
    {
        _settings = settings;
        _openAIService = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = _settings.ApiKey
        });
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
        
        var chatRequest = new ChatCompletionCreateRequest
        {
            Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo,
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(SystemPrompt),
                ChatMessage.FromUser(userMessage)
            },
            MaxTokens = _settings.MaxTokens,
            Temperature = (float)_settings.Temperature
        };

        var response = await _openAIService.ChatCompletion.CreateCompletion(chatRequest);
        
        if (response?.Successful != true || response.Choices?.FirstOrDefault()?.Message?.Content == null)
        {
            _logger.LogWarning("Invalid response from OpenAI for transaction {Id}", transaction.Id);
            throw new InvalidOperationException($"OpenAI API returned invalid response: {response?.Error?.Message}");
        }

        var category = response.Choices.First().Message.Content!.Trim();
        return ValidateCategory(category);
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
}