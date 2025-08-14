using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Components;

public class Categorizer(
    IChatClient chatClient,
    IDistributedCache distributedCache,
    ICategoriesService categoriesService,
    AIFunctionService aIFunctionService,
    LlmOptions settings)
{
    private const string CategorizerPromptCacheKey = "CategorizerPrompt";

    private readonly ChatOptions _chatOptions = new()
    {
        Tools = [
            aIFunctionService.GetSubCategories(),
            aIFunctionService.GetMainCategories()
        ]
    };

    public async Task<Categorization?> CategorizeAsync(RawTransaction transaction)
    {
        List<ChatMessage> chatMessages = await GetChatMessagesAsync(transaction);
        Categorization? categorization = await InternalCategorizeAsync(chatMessages, _chatOptions);

        return categorization;
    }

    private async Task<List<ChatMessage>> GetChatMessagesAsync(RawTransaction transaction)
    {
        ChatMessage systemMessage = await CreateSystemMessageAsync();
        ChatMessage userMessage = CreateUserMessage(transaction);
        List<ChatMessage> chatMessages = [systemMessage, userMessage];
        
        return chatMessages;
    }

    private async Task<Categorization?> InternalCategorizeAsync(List<ChatMessage> chatHistory, ChatOptions chatOptions)
    {
        // Retry logic with validation
        int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            ChatResponse<Categorization> response = await chatClient.GetResponseAsync<Categorization>(chatHistory, chatOptions);
            Categorization categorization = response.Result;

            ValidationResult validationResult = categoriesService.ValidateCategorization(categorization);

            if (validationResult.IsValid)
            {
                return categorization;
            }

            // Add validation error to chat history for retry
            if (attempt < maxRetries - 1)
            {
                //chatHistory.Add(new ChatMessage(ChatRole.Assistant, $"Selected: {categorization.MainCategory} - {categorization.SubCategory}"));
                chatHistory.Add(new ChatMessage(ChatRole.User, $"Error: {validationResult.ErrorMessage}. Please provide a valid categorization."));
            }
        }

        return null; // Return null if all retries fail
    }

    private async Task<ChatMessage> CreateSystemMessageAsync()
    {
        string categorizerPrompt = await ReadCategorizerPromptAsync();
        ChatMessage chatMessage = new(ChatRole.System, categorizerPrompt);

        return chatMessage;
    }

    private async Task<string> ReadCategorizerPromptAsync()
    {
        string? cachedPrompt = await distributedCache.GetStringAsync(CategorizerPromptCacheKey);
        if (cachedPrompt is not null)
            return cachedPrompt;

        string categorizerPromptPath = Path.Combine(AppContext.BaseDirectory, settings.Prompts.Path, settings.Prompts.CategorizerDeveloperMessage);
        string categorizerPrompt = await File.ReadAllTextAsync(categorizerPromptPath);

        await distributedCache.SetStringAsync(CategorizerPromptCacheKey, categorizerPrompt);

        return categorizerPrompt;
    }
    
    private static ChatMessage CreateUserMessage(RawTransaction transaction)
    {
        string userPrompt = CreateUserPrompt(transaction);
        ChatMessage userChatMessage = new(ChatRole.User, userPrompt);

        return userChatMessage;
    }

    private static string CreateUserPrompt(RawTransaction transaction)
    {
        string userPrompt = $"description: \"{transaction.Description.Trim()}\", title: \"{transaction.Title.Trim()}\"";
        if (string.IsNullOrWhiteSpace(transaction.Receiver) is not true)
        {
            userPrompt += $", receiver: \"{transaction.Receiver.Trim()}\"";
        }

        return userPrompt;
    }
}