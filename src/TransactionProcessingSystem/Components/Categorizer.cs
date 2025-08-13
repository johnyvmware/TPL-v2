using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Models.Categories;
using TransactionProcessingSystem.Tools;

namespace TransactionProcessingSystem.Components;

public class Categorizer(
    ChatClient chatClient,
    IOptions<LlmOptions> llmSettings)
{
    private readonly LlmOptions llmSettings = llmSettings.Value;

    public async Task<Transaction?> CategorizeTransactionAsync(RawTransaction transaction)
    {
        Categorization? categorization = await InternalCategorizeTransactionAsync(transaction);

        if (categorization != null && CategoryValidator.IsValidCategorization(categorization))
        {
            return new Transaction
            {
                Date = transaction.Date,
                Description = transaction.Description,
                Title = transaction.Title,
                Receiver = transaction.Receiver,
                Amount = transaction.Amount,
                Categorization = categorization
            };
        }

        return null;
    }

    private async Task<Categorization?> InternalCategorizeTransactionAsync(RawTransaction transaction)
    {
        List<ChatMessage> chatMessages = await CreateChatMessages(transaction);
        ChatCompletionOptions chatOptions = await CreateChatCompletionOptions();

        bool requiresAction;
        Categorization? categorization = null;

        do
        {
            requiresAction = false;
            ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages, chatOptions);

            switch (chatCompletion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    {
                        chatMessages.Add(new AssistantChatMessage(chatCompletion));
                        categorization = JsonSerializer.Deserialize<Categorization>(chatCompletion.Content[0].Text);

                        break;
                    }

                case ChatFinishReason.ToolCalls:
                    {
                        // First, add the assistant message with tool calls to the conversation history.
                        chatMessages.Add(new AssistantChatMessage(chatCompletion));

                        // Then, add a new tool message for each tool call that is resolved.
                        foreach (ChatToolCall toolCall in chatCompletion.ToolCalls)
                        {
                            switch (toolCall.FunctionName)
                            {
                                case nameof(CategoryRegistry.GetSubCategories):
                                    {
                                        // The arguments that the model wants to use to call the function are specified as a
                                        // stringified JSON object based on the schema defined in the tool definition. Note that
                                        // the model may hallucinate arguments too. Consequently, it is important to do the
                                        // appropriate parsing and validation before calling the function.
                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                        bool hasMainCategory = argumentsJson.RootElement.TryGetProperty("mainCategory", out JsonElement mainCategory);

                                        if (!hasMainCategory)
                                        {
                                            throw new ArgumentNullException(nameof(mainCategory), "The main category argument is required.");
                                        }

                                        var toolResult = CategoryRegistry.GetSubCategories(mainCategory.ToString());
                                        chatMessages.Add(new ToolChatMessage(toolCall.Id, string.Join(", ", toolResult)));
                                        break;
                                    }

                                default:
                                    {
                                        // Handle other unexpected calls.
                                        throw new NotImplementedException();
                                    }
                            }
                        }

                        requiresAction = true;
                        break;
                    }

                case ChatFinishReason.Length:
                    throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                case ChatFinishReason.ContentFilter:
                    throw new NotImplementedException("Omitted content due to a content filter flag.");

                case ChatFinishReason.FunctionCall:
                    throw new NotImplementedException("Deprecated in favor of tool calls.");

                default:
                    throw new NotImplementedException(chatCompletion.FinishReason.ToString());
            }

        } while (requiresAction);

        return categorization;
    }

    private async Task<List<ChatMessage>> CreateChatMessages(RawTransaction transaction)
    {
        DeveloperChatMessage systemMessage = await CreateDeveloperChatMessageAsync();
        UserChatMessage userMessage = CreateUserChatMessage(transaction);

        return
        [
            systemMessage,
            userMessage
        ];
    }

    private async Task<DeveloperChatMessage> CreateDeveloperChatMessageAsync()
    {
        string categorizerPrompt = await ReadCategorizerPromptAsync();
        DeveloperChatMessage developerChatMessage = new(categorizerPrompt);

        return developerChatMessage;
    }

    private async Task<string> ReadCategorizerPromptAsync()
    {
        string categorizerPromptPath = Path.Combine(AppContext.BaseDirectory, llmSettings.Prompts.Path, llmSettings.Prompts.CategorizerDeveloperMessage);
        string categorizerPrompt = await File.ReadAllTextAsync(categorizerPromptPath);

        return categorizerPrompt;
    }

    private static UserChatMessage CreateUserChatMessage(RawTransaction transaction)
    {
        string userPrompt = CreateUserPrompt(transaction);
        UserChatMessage userChatMessage = new(userPrompt);

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

    private async Task<ChatCompletionOptions> CreateChatCompletionOptions()
    {
        string jsonSchemaName = Path.GetFileNameWithoutExtension(llmSettings.StructuredOutputs.Categorizer);
        BinaryData jsonSchemaBinaryData = await GetJsonSchemaAsync();

        return new ChatCompletionOptions
        {
            Temperature = llmSettings.OpenAI.Temperature,
            MaxOutputTokenCount = llmSettings.OpenAI.MaxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: jsonSchemaName,
                jsonSchema: jsonSchemaBinaryData,
                jsonSchemaIsStrict: true),
            Tools = { CategoryRegistry.GetSubCategoriesTool },
        };
    }

    private async Task<BinaryData> GetJsonSchemaAsync()
    {
        string schemaPath = Path.Combine(AppContext.BaseDirectory, llmSettings.StructuredOutputs.Path, llmSettings.StructuredOutputs.Categorizer);
        string schema = await File.ReadAllTextAsync(schemaPath, Encoding.UTF8);

        BinaryData binarySchema = BinaryData.FromString(schema);

        return binarySchema;
    }
}