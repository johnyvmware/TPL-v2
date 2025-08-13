using Microsoft.Extensions.AI;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Tools;

namespace TransactionProcessingSystem.Components;

public class CategorizerV2(
    IChatClient chatClient)
{
    public async Task CategorizeTransactionAsync(RawTransaction transaction)
    {
        var chatOptions = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create((string mainCategory) => {
                    return CategoryDefinitions.GetSubCategories(mainCategory);
                },
                "get_sub_categories",
                "Get sub categories for a given main category"),

                AIFunctionFactory.Create(() => {
                    return CategoryDefinitions.Categories.Keys;
                },
                "get_main_categories",
                "Get main categories")]
        };

        List<ChatMessage> chatHistory = [new(ChatRole.System,
        """
        You are a helpful assistant that categorizes transactions into main categories and subcategories.
        """)];

        string userPrompt = $"description: \"{transaction.Description.Trim()}\", title: \"{transaction.Title.Trim()}\"";
        if (string.IsNullOrWhiteSpace(transaction.Receiver) is not true)
        {
            userPrompt += $", receiver: \"{transaction.Receiver.Trim()}\"";
        }

        chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

        var response = await chatClient.GetResponseAsync<Categorization>(chatHistory, chatOptions);
        Categorization test = response.Result;
    }
}