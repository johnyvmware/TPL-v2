using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using TransactionProcessingSystem.Configuration;

public class OpenAiCategorizer
{
    private readonly ILogger<OpenAiCategorizer> _logger;
    private readonly ChatClient _openAiClient;

    public OpenAiCategorizer(
        ILogger<OpenAiCategorizer> logger,
        IOptions<OpenAISecrets> openAiSecrets,
        IOptions<OpenAISettings> openAiSettings)
    {
        _logger = logger;
        _openAiClient = new ChatClient(openAiSettings.Value.Model, openAiSecrets.Value.ApiKey);

        // response client cant have strucuterd output but they offer tools
        //var responseClient = new OpenAIResponseClient(openAiSettings.Value.Model, openAiSecrets.Value.ApiKey);
    }

    public async Task CategorizeTransactionAsync(BankTransaction transaction)
    {
        var options = CreateTransactionTitleExtractionOptions();
        var prompt = $"""
            Please provide a clean, standardized transaction title that clearly identifies the transaction purpose:
            
            Title: {transaction.Title?.Trim()}

            """;

        var userMessage = new UserChatMessage(prompt);

        ChatCompletion completion = await _openAiClient.CompleteChatAsync([userMessage], options);

        using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);

        Console.WriteLine($"Final answer: {structuredJson.RootElement.GetProperty("final_answer")}");
        Console.WriteLine("Reasoning steps:");

        foreach (JsonElement stepElement in structuredJson.RootElement.GetProperty("steps").EnumerateArray())
        {
            Console.WriteLine($"  - Explanation: {stepElement.GetProperty("explanation")}");
            Console.WriteLine($"    Output: {stepElement.GetProperty("output")}");
        }
    }

        private static ChatCompletionOptions CreateTransactionTitleExtractionOptions()
    {
        return new ChatCompletionOptions
        {
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
                            },
                            "extraction_notes": {
                                "type": "string",
                                "description": "Short, informative reasoning on how the standardized title was determined from the provided noisy transaction title.",
                                "minLength": 1,
                                "maxLength": 256
                            }
                        },
                        "required": [
                            "standardized_title",
                            "extraction_notes"
                        ],
                        "additionalProperties": false
                    }
                    """u8.ToArray()),
                jsonSchemaIsStrict: true)
        };
    }
}