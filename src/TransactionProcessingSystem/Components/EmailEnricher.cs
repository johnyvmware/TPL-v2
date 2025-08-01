using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Azure.Identity;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;
using System.Runtime.CompilerServices;

namespace TransactionProcessingSystem.Components;

public class EmailEnricher : ProcessorBase<Transaction, Transaction>
{
    private readonly GraphServiceClient _graphClient;
    private readonly MicrosoftGraphSettings _settings;
    private readonly MicrosoftGraphSecrets _secrets;
    private static readonly Regex AmountRegex = new(@"\$?(\d+(?:\.\d{2})?)", RegexOptions.Compiled);

    public EmailEnricher(
        IOptions<MicrosoftGraphSettings> settings,
        IOptions<MicrosoftGraphSecrets> secrets,
        ILogger<EmailEnricher> logger,
        int boundedCapacity = 100)
        : base(logger, boundedCapacity)
    {
        _settings = settings.Value;
        _secrets = secrets.Value;
        _graphClient = CreateGraphClient();
    }

    protected override async Task<Transaction> ProcessAsync(Transaction transaction)
    {
        _logger.LogDebug("Enriching transaction {Id} with email data", transaction.Id);

        try
        {
            var matchedEmail = await FindBestEmailMatchAsync(transaction);

            var enrichedTransaction = transaction with
            {
                EmailSubject = matchedEmail?.Subject,
                EmailSnippet = matchedEmail?.Snippet,
                Status = ProcessingStatus.EmailEnriched
            };

            if (matchedEmail != null)
            {
                _logger.LogDebug("Enriched transaction {Id} with email: {Subject}",
                    transaction.Id, matchedEmail.Subject);
            }
            else
            {
                _logger.LogDebug("No matching email found for transaction {Id}", transaction.Id);
            }

            return enrichedTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enrich transaction {Id} with email data", transaction.Id);
            // Continue processing without email enrichment
            return transaction with { Status = ProcessingStatus.EmailEnriched };
        }
    }

    private GraphServiceClient CreateGraphClient()
    {
        try
        {
            var credential = new ClientSecretCredential(
                _secrets.TenantId,
                _secrets.ClientId,
                _secrets.ClientSecret);

            return new GraphServiceClient(credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Graph client");
            throw;
        }
    }

    private async IAsyncEnumerable<EmailMatch> SearchRelevantEmailsAsync(
        Transaction transaction,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var startDate = transaction.Date.AddDays(-_settings.EmailSearchDays);
        var endDate = transaction.Date.AddDays(_settings.EmailSearchDays);

        var filter = $"receivedDateTime ge {startDate:yyyy-MM-ddTHH:mm:ssZ} and receivedDateTime le {endDate:yyyy-MM-ddTHH:mm:ssZ}";

        Microsoft.Graph.Models.MessageCollectionResponse? messages = null;
        try
        {
            var messagesRequest = _graphClient.Me.Messages.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = filter;
                requestConfiguration.QueryParameters.Select = new[] { "subject", "bodyPreview", "receivedDateTime" };
                requestConfiguration.QueryParameters.Top = 50;
            }, cancellationToken);

            messages = await messagesRequest;

            _logger.LogDebug("Found {Count} emails in date range for transaction {Id}",
                messages?.Value?.Count ?? 0, transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search emails for transaction {Id}", transaction.Id);
            yield break; // Exit early on error
        }

        // Stream results as async enumerator instead of collecting first
        if (messages?.Value != null)
        {
            foreach (var message in messages.Value)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (message.Subject != null && message.BodyPreview != null)
                {
                    yield return new EmailMatch
                    {
                        Subject = message.Subject,
                        Snippet = message.BodyPreview,
                        ReceivedDateTime = message.ReceivedDateTime?.DateTime ?? DateTime.MinValue
                    };
                }
            }
        }
    }

    private async Task<EmailMatch?> FindBestEmailMatchAsync(Transaction transaction)
    {
        EmailMatch? bestEmail = null;
        var bestScore = 0.0;

        // Use async enumerator instead of await foreach to stream process candidates
        await foreach (var email in SearchRelevantEmailsAsync(transaction))
        {
            var score = CalculateMatchScore(transaction, email);
            if (score > bestScore)
            {
                bestEmail = email;
                bestScore = score;
            }
        }

        return bestScore > 0 ? bestEmail : null;
    }

    private double CalculateMatchScore(Transaction transaction, EmailMatch email)
    {
        double score = 0;

        // Check for amount match in subject and snippet
        var emailText = $"{email.Subject} {email.Snippet}".ToLower();
        var amounts = AmountRegex.Matches(emailText)
            .Select(m => decimal.TryParse(m.Groups[1].Value, out var amount) ? amount : 0)
            .Where(a => a > 0)
            .ToList();

        if (amounts.Any(a => Math.Abs(a - transaction.Amount) < 0.01m))
        {
            score += 50; // Exact amount match
        }
        else if (amounts.Any(a => Math.Abs(a - transaction.Amount) < 1.0m))
        {
            score += 25; // Close amount match
        }

        // Check for description keyword matches
        var transactionKeywords = ExtractKeywords(transaction.CleanDescription ?? transaction.Description);
        var emailKeywords = ExtractKeywords(emailText);

        var commonKeywords = transactionKeywords.Intersect(emailKeywords, StringComparer.OrdinalIgnoreCase);
        score += commonKeywords.Count() * 5;

        // Prefer emails closer to transaction date
        var timeDifference = Math.Abs((email.ReceivedDateTime - transaction.Date).TotalDays);
        score += Math.Max(0, 10 - timeDifference);

        // Boost score for financial-related emails
        if (IsFinancialEmail(emailText))
        {
            score += 10;
        }

        return score;
    }

    private List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Select(w => w.Trim('.', ',', '!', '?', ';', ':'))
            .Where(w => !IsStopWord(w))
            .ToList();

        return words;
    }

    private bool IsStopWord(string word)
    {
        var stopWords = new[] { "the", "and", "for", "are", "but", "not", "you", "all", "can", "her", "was", "one", "our", "had", "have", "has", "this", "that", "with", "from" };
        return stopWords.Contains(word.ToLower());
    }

    private bool IsFinancialEmail(string emailText)
    {
        var financialKeywords = new[] { "payment", "charge", "receipt", "invoice", "transaction", "purchase", "bill", "card", "account", "bank" };
        return financialKeywords.Any(keyword => emailText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}