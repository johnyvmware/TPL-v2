using System.Transactions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Components;

public class CategorizerV2(
    IChatClient chatClient)
{
    public async Task CategorizeTransactionAsync()
    {
        var response = await chatClient.GetResponseAsync("Who are you?");
    }
}