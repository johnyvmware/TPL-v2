using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class Categorizer(
    ChatClient chatClient,
    IOptions<LlmSettings> llmSettings,
    ILogger<Categorizer> logger)
{
    private readonly ILogger<Categorizer> _logger = logger;
    private readonly LlmSettings _llmSettings = llmSettings.Value;
    private readonly ChatClient _openAiClient = chatClient;

    public async Task<Transaction?> CategorizeTransactionAsync(RawTransaction transaction)
    {
        IEnumerable<ChatMessage> chatMessages = await CreateChatMessages(transaction);
        ChatCompletionOptions chatOptions = await CreateChatCompletionOptions();
        ChatCompletion chatCompletion = await _openAiClient.CompleteChatAsync(chatMessages, chatOptions);

        if (chatCompletion.Content[0].Text is not null)
        {
            ExpenseCategorization? expenseCategorization = JsonSerializer.Deserialize<ExpenseCategorization>(chatCompletion.Content[0].Text);

            if (expenseCategorization != null)
            {
                return new Transaction
                {
                    Date = transaction.Date,
                    Description = transaction.Description,
                    Title = transaction.Title,
                    Receiver = transaction.Receiver,
                    Amount = transaction.Amount,
                    Categorization = expenseCategorization
                };
            }
        }

        _logger.LogDebug("Failed to extract a valid standardized title from the response.");
        return null;
    }

    private async Task<List<ChatMessage>> CreateChatMessages(RawTransaction transaction)
    {
        SystemChatMessage systemMessage = await CreateSystemChatMessageAsync();
        UserChatMessage userMessage = CreateUserChatMessage(transaction);

        return
        [
            systemMessage,
            userMessage
        ];
    }

    private async Task<SystemChatMessage> CreateSystemChatMessageAsync()
    {
        string categorizerPromptPath = Path.Combine(AppContext.BaseDirectory, _llmSettings.Prompts.Path, _llmSettings.Prompts.CategorizerDeveloperMessage);
        string categorizerPrompt = await File.ReadAllTextAsync(categorizerPromptPath).ConfigureAwait(false);

        return new SystemChatMessage(categorizerPrompt);
    }

    private static UserChatMessage CreateUserChatMessage(RawTransaction transaction)
    {
        string userPrompt = $"description: \"{transaction.Description.Trim()}\", title: \"{transaction.Title.Trim()}\"";
        if (string.IsNullOrWhiteSpace(transaction.Receiver) is not true)
        {
            userPrompt += $", receiver: \"{transaction.Receiver.Trim()}\"";
        }

        return new UserChatMessage(userPrompt);
    }

    private async Task<ChatCompletionOptions> CreateChatCompletionOptions()
    {
        string jsonSchemaName = Path.GetFileNameWithoutExtension(_llmSettings.StructuredOutputs.Categorizer);
        BinaryData jsonSchemaBinaryData = await GetJsonSchemaAsync().ConfigureAwait(false);

        return new ChatCompletionOptions
        {
            Temperature = _llmSettings.OpenAI.Temperature,
            MaxOutputTokenCount = _llmSettings.OpenAI.MaxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: jsonSchemaName,
                jsonSchema: jsonSchemaBinaryData,
                jsonSchemaIsStrict: true)
        };
    }

    // This is still wrong, somehow the json is not picked up correctly
    private async Task<BinaryData> GetJsonSchemaAsync()
    {
        string schemaPath = Path.Combine(AppContext.BaseDirectory, _llmSettings.StructuredOutputs.Path, _llmSettings.StructuredOutputs.Categorizer);
        string fileName = Path.GetFileNameWithoutExtension(schemaPath);

        // Read the JSON file
        await using FileStream stream = File.OpenRead(schemaPath);

        // Parse as JsonDocument - possible to define a schema file as an object in C#
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);

        // Convert to binary
        byte[] binaryData = JsonSerializer.SerializeToUtf8Bytes(doc.RootElement);

        return await BinaryData.FromStreamAsync(stream);
    }
}