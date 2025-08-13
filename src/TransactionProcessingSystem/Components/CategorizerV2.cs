using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Tools;

namespace TransactionProcessingSystem.Components;

public class CategorizerV2(
    IChatClient chatClient,
    IOptions<LlmOptions> llmSettings)
{
    public async Task CategorizeTransactionAsync(RawTransaction transaction)
    {
        var chatOptions = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create((string mainCategory) => {
                    return CategoryRegistry.GetSubCategories(mainCategory);
                },
                "get_sub_categories",
                "Get sub categories for a given main category"),

                AIFunctionFactory.Create(() => {
                    return CategoryRegistry.GetMainCategories();
                },
                "get_main_categories",
                "Get main categories")]
        };

        ChatMessage systemMessage = await CreateSystemMessageAsync();
        ChatMessage userMessage = CreateUserMessage(transaction);
        List<ChatMessage> chatHistory = [systemMessage, userMessage];

        ChatResponse<Categorization> response = await chatClient.GetResponseAsync<Categorization>(chatHistory, chatOptions);
        Categorization categorization = response.Result; // Home and Daily categories seem to be really close to each other for the model space, and I get mixed results
    }

    private async Task<ChatMessage> CreateSystemMessageAsync()
    {
        string categorizerPrompt = await ReadCategorizerPromptAsync();
        ChatMessage chatMessage = new(ChatRole.System, categorizerPrompt);

        return chatMessage;
    }

    private async Task<string> ReadCategorizerPromptAsync()
    {
        string categorizerPromptPath = Path.Combine(AppContext.BaseDirectory, llmSettings.Value.Prompts.Path, llmSettings.Value.Prompts.CategorizerDeveloperMessage);
        string categorizerPrompt = await File.ReadAllTextAsync(categorizerPromptPath);

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