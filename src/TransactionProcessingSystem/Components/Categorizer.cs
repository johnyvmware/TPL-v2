using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;
using TransactionProcessingSystem.Services.Categorizer;

namespace TransactionProcessingSystem.Components;

public class Categorizer(
    IChatClient chatClient,
    IDistributedCache distributedCache,
    ICategoryValidator categoryValidator,
    AIFunctionService aIFunctionService,
    LlmOptions settings)
{
    private const string DeveloperPromptCacheKey = "CategorizerDeveloperPrompt";
    private const string UserPromptCacheKey = "CategorizerUserPrompt";

    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [
            aIFunctionService.GetSubCategoriesAIFunction(),
            aIFunctionService.GetMainCategoriesAIFunction()
        ],
    };

    public async Task<Transaction> CategorizeAsync(Transaction transaction)
    {
        List<ChatMessage> chatMessages = await GetChatMessagesAsync(transaction);
        CategoryAssignment? categoryAssignment = await CategorizeAsync(chatMessages);

        return transaction with { CategoryAssignment = categoryAssignment };
    }

    private async Task<List<ChatMessage>> GetChatMessagesAsync(Transaction transaction)
    {
        ChatMessage systemMessage = await CreateSystemMessageAsync();
        ChatMessage userMessage = await CreateUserMessage(transaction);
        List<ChatMessage> chatMessages = [systemMessage, userMessage];

        return chatMessages;
    }

    private async Task<CategoryAssignment?> CategorizeAsync(List<ChatMessage> chatHistory)
    {
        // Retry logic with validation
        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            ChatResponse<CategoryAssignment> response = await chatClient.GetResponseAsync<CategoryAssignment>(chatHistory, _chatOptions);
            CategoryAssignment categoryAssignment = response.Result;
            CategoryAssignmentResult validationResult = categoryValidator.Validate(categoryAssignment);

            if (validationResult.IsValid)
            {
                return categoryAssignment;
            }

            // Add validation error to chat history for retry OR WORK WITH ID FROM AI EXTENSIONS!
            if (attempt < maxRetries - 1)
            {
                chatHistory.Add(new ChatMessage(ChatRole.User, $"Error: {validationResult.ErrorMessage}. Please provide a valid categorization."));
            }
        }

        return null; // Return null if all retries fail
    }

    private async Task<ChatMessage> CreateSystemMessageAsync()
    {
        string categorizerPrompt = await ReadDeveloperPromptAsync();
        ChatMessage chatMessage = new(ChatRole.System, categorizerPrompt);

        return chatMessage;
    }

    private async Task<string> ReadDeveloperPromptAsync()
    {
        string? cachedPrompt = await distributedCache.GetStringAsync(DeveloperPromptCacheKey);
        if (cachedPrompt is not null)
        {
            return cachedPrompt;
        }

        string categorizerPromptPath = Path.Combine(AppContext.BaseDirectory, settings.Prompts.Path, settings.Prompts.CategorizerDeveloperMessage);
        string categorizerPrompt = await File.ReadAllTextAsync(categorizerPromptPath);

        await distributedCache.SetStringAsync(DeveloperPromptCacheKey, categorizerPrompt);

        return categorizerPrompt;
    }

    private async Task<ChatMessage> CreateUserMessage(Transaction transaction)
    {
        string userPrompt = await ReadUserPromptAsync(transaction);
        ChatMessage userChatMessage = new(ChatRole.User, userPrompt);

        return userChatMessage;
    }

    private async Task<string> ReadUserPromptAsync(Transaction transaction)
    {
        string? cachedPrompt = await distributedCache.GetStringAsync(UserPromptCacheKey);
        if (cachedPrompt is not null)
        {
            return cachedPrompt;
        }

        string categorizerPromptPath = Path.Combine(AppContext.BaseDirectory, settings.Prompts.Path, settings.Prompts.CategorizerUserMessage);
        string categorizerPrompt = await File.ReadAllTextAsync(categorizerPromptPath);
        categorizerPrompt = categorizerPrompt.Replace("{TransactionDescription}", transaction.Describe());

        await distributedCache.SetStringAsync(UserPromptCacheKey, categorizerPrompt);

        return categorizerPrompt;
    }
}