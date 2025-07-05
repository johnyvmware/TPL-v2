using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Azure.Identity;
using TransactionProcessingSystem.Configuration;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Agents;

public class EmailEnricherAgent : AgentBase<Transaction, Transaction>
{
    private readonly GraphServiceClient _graphClient;
    private readonly MicrosoftGraphSettings _settings;
    private static readonly Regex AmountRegex = new(@"\$?(\d+(?:\.\d{2})?)", RegexOptions.Compiled);

    public EmailEnricherAgent(
        MicrosoftGraphSettings settings,
        ILogger<EmailEnricherAgent> logger,
        int boundedCapacity = 100) 
        : base(logger, boundedCapacity)
    {
        _settings = settings;
        _graphClient = CreateGraphClient();
    }

    protected override async Task<Transaction> ProcessAsync(Transaction transaction)
    {
        _logger.LogDebug("Enriching transaction {Id} with email data", transaction.Id);

        try
        {
            var emails = await SearchRelevantEmails(transaction);
            var matchedEmail = FindBestEmailMatch(transaction, emails);

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
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret);

            return new GraphServiceClient(credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Graph client");
            throw;
        }
    }

    private async Task<IEnumerable<EmailMatch>> SearchRelevantEmails(Transaction transaction)
    {
        try
        {
            var startDate = transaction.Date.AddDays(-_settings.EmailSearchDays);
            var endDate = transaction.Date.AddDays(_settings.EmailSearchDays);

            var filter = $"receivedDateTime ge {startDate:yyyy-MM-ddTHH:mm:ssZ} and receivedDateTime le {endDate:yyyy-MM-ddTHH:mm:ssZ}";

            var messagesRequest = _graphClient.Me.Messages.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = filter;
                requestConfiguration.QueryParameters.Select = new[] { "subject", "bodyPreview", "receivedDateTime" };
                requestConfiguration.QueryParameters.Top = 50;
            });

            var messages = await messagesRequest;

            var emailMatches = messages?.Value?
                .Where(m => m.Subject != null && m.BodyPreview != null)
                .Select(m => new EmailMatch
                {
                    Subject = m.Subject!,
                    Snippet = m.BodyPreview!,
                    ReceivedDateTime = m.ReceivedDateTime?.DateTime ?? DateTime.MinValue
                })
                .ToList() ?? new List<EmailMatch>();

            _logger.LogDebug("Found {Count} emails in date range for transaction {Id}", 
                emailMatches.Count, transaction.Id);

            return emailMatches;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search emails for transaction {Id}", transaction.Id);
            return Enumerable.Empty<EmailMatch>();
        }
    }

    private EmailMatch? FindBestEmailMatch(Transaction transaction, IEnumerable<EmailMatch> emails)
    {
        if (!emails.Any())
            return null;

        var candidates = emails.ToList();
        var bestMatch = candidates
            .Select(email => new
            {
                Email = email,
                Score = CalculateMatchScore(transaction, email)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch?.Email;
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