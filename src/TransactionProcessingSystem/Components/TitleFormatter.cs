using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class TitleFormatter(
    ChatClient chatClient,
    IOptions<OpenAISettings> openAiSettings,
    ILogger<TitleFormatter> logger)
{
    private readonly ILogger<TitleFormatter> _logger = logger;
    private readonly OpenAISettings _openAiSettings = openAiSettings.Value;
    private readonly ChatClient _openAiClient = chatClient;
    
    private const string JsonSchemaFormatName = "standardized_title";
    private const string SystemPrompt = """
        Clean and standardize noisy bank transaction titles.

        You will receive a single, potentially noisy or unstructured bank transaction title. Your task is to analyze and understand the underlying meaning of this title, correcting misspellings, removing unnecessary noise (like transaction codes, random numbers, or irrelevant symbols), and rewriting it into a concise, clear, and standardized label that best describes the true nature of the transaction (e.g., merchant name, transaction type). Do not guess if the information is ambiguous—simply output the most accurate, succinct description based on what is given.

        Always reason carefully about which text elements are significant before producing your cleaned title. Only then, in your final output, provide the standardized transaction title as a single short phrase.

        # Example
        Input: PAYPAL *UBER 402-935-7733 CA 0000000000000000 XX000
        Reasoning:
        - 'PAYPAL *UBER' indicates a PayPal UBER ride transaction.
        - The long number, phone number, and trailing codes are irrelevant transaction metadata.
        - The core merchant/service is "Uber."

        Output: Uber
        """;

    public async Task<Transaction?> CategorizeTransactionAsync(RawTransaction transaction)
    {
        IEnumerable<ChatMessage> messages = CreateChatMessages(transaction);
        ChatCompletionOptions options = CreateChatCompletionOptions();

        ChatCompletion completion = await _openAiClient.CompleteChatAsync(messages, options);

        using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);

        if (IsValidStandardizedTitle(structuredJson, out JsonElement standardizedTitleElement))
        {
            string standardizedTitle = standardizedTitleElement.GetString()!;

            return new Transaction
            {
                Date = transaction.Date,
                Description = standardizedTitle,
                Amount = transaction.Amount
            };
        }

        _logger.LogDebug("Failed to extract a valid standardized title from the response.");
        return null;
    }

    private static bool IsValidStandardizedTitle(JsonDocument structuredJson, out JsonElement standardizedTitleElement)
    {
        if (!structuredJson.RootElement.TryGetProperty(JsonSchemaFormatName, out standardizedTitleElement))
            return false;

        if (standardizedTitleElement.ValueKind != JsonValueKind.String)
            return false;

        if (string.IsNullOrWhiteSpace(standardizedTitleElement.GetString()))
            return false;
 
        return true;
    }

    private static IEnumerable<ChatMessage> CreateChatMessages(RawTransaction transaction)
    {
        string userPrompt = $"{transaction.Description?.Trim()} {transaction.Title?.Trim()}";

        yield return new SystemChatMessage(SystemPrompt);
        yield return new UserChatMessage(userPrompt);
    }

    private  ChatCompletionOptions CreateChatCompletionOptions()
    {
        return new ChatCompletionOptions
        {
            Temperature = _openAiSettings.Temperature,
            MaxOutputTokenCount = _openAiSettings.MaxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "transaction_title_extraction",
                jsonSchema: BinaryData.FromBytes("""
                    {
                        "type": "object",
                        "properties": {
                            "standardized_title": {
                                "type": "string",
                                "description": "A clean, standardized transaction title that clearly and concisely identifies the transaction's purpose.",
                                "minLength": 1,
                                "maxLength": 128
                            }
                        },
                        "required": [
                            "standardized_title"
                        ],
                        "additionalProperties": false
                    }
                    """u8.ToArray()),
                jsonSchemaIsStrict: true)
        };
    }
}