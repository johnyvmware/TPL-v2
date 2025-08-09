using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Responses;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using TransactionProcessingSystem.Tools;

namespace TransactionProcessingSystem.Components;

public class Categorizer(
    OpenAIResponseClient openAiResponseClient,
    IOptions<LlmSettings> llmSettings,
    ILogger<Categorizer> logger)
{
    private readonly LlmSettings llmSettings = llmSettings.Value;

    // so here use tool calling
    // maybe first let the model pick right tool for main cateogry and this would return a list of sub categories
    // and model would then pick the right sub category
    // maybe this we response api? but response api is still experimental
    public async Task<Transaction?> CategorizeTransactionAsync(RawTransaction transaction)
    {
        //IEnumerable<ChatMessage> chatMessages = await CreateChatMessages(transaction);
        //ChatCompletionOptions chatOptions = await CreateChatCompletionOptions();
        //ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages, chatOptions);

        List<MessageResponseItem> responseItems = await CreateResponseItems(transaction);
        ResponseCreationOptions responseOptions = CreateResponseOptions();
        OpenAIResponse openAIResponse = await openAiResponseClient.CreateResponseAsync(responseItems, responseOptions);

/*         if (chatCompletion.Content[0].Text is not null)
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
        } */

        logger.LogDebug("Failed to extract a valid standardized title from the response.");
        return null;
    }

    private ResponseCreationOptions CreateResponseOptions()
    {
        // 'country' must be an ISO 3166-1 code (https://en.wikipedia.org/wiki/ISO_3166-1)
        // 4.1-nano-2025-04-14 is not available for web search
        // 5-nano-2025-08-07 does not support temperature
        var webSearchUserLocation = WebSearchUserLocation.CreateApproximateLocation("PL", "Mazowieckie", "Warsaw");
        var webSearchContextSize = WebSearchContextSize.Medium;
        var responseOptions = new ResponseCreationOptions
        {
            //Temperature = llmSettings.OpenAI.Temperature,
            MaxOutputTokenCount = llmSettings.OpenAI.MaxTokens,
            Tools =
            {
                ResponseTool.CreateWebSearchTool(webSearchUserLocation, webSearchContextSize),
                CategoriesTool.GetHomeCategoriesTool(),
                CategoriesTool.GetDailyCategoriesTool(),
                CategoriesTool.GetEducationCategoriesTool(),
                CategoriesTool.GetTransportCategoriesTool(),
                CategoriesTool.GetEntertainmentCategoriesTool(),
                CategoriesTool.GetHealthCategoriesTool(),
                CategoriesTool.GetPersonalCategoriesTool(),
                CategoriesTool.GetOtherCategoriesTool()
            },
        };

        return responseOptions;
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

    private async Task<List<MessageResponseItem>> CreateResponseItems(RawTransaction transaction)
    {
        MessageResponseItem developerMessage = await CreateResponsesDeveloperMessage(transaction);
        MessageResponseItem userMessage = CreateResponsesUserMessage(transaction);

        return
        [
            developerMessage,
            userMessage
        ];
    }

    private async Task<DeveloperChatMessage> CreateDeveloperChatMessageAsync()
    {
        string categorizerPrompt = await ReadCategorizerPromptAsync();
        DeveloperChatMessage developerChatMessage = new(categorizerPrompt);

        return developerChatMessage;
    }

    private async Task<MessageResponseItem> CreateResponsesDeveloperMessage(RawTransaction transaction)
    {
        string categorizerPrompt = await ReadCategorizerPromptAsync();
        MessageResponseItem messageResponseItem = ResponseItem.CreateDeveloperMessageItem(categorizerPrompt);

        return messageResponseItem;
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

    private static MessageResponseItem CreateResponsesUserMessage(RawTransaction transaction)
    {
        string userPrompt = CreateUserPrompt(transaction);
        MessageResponseItem userMessageItem = ResponseItem.CreateUserMessageItem(userPrompt);

        return userMessageItem;
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
                jsonSchemaIsStrict: true)
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