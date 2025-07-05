using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TransactionProcessingSystem.Models;

namespace TransactionProcessingSystem.Agents;

public class TransactionProcessorAgent : AgentBase<Transaction, Transaction>
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SpecialCharsRegex = new(@"[^\w\s\-\.\,\$\&\@\#\%\(\)]", RegexOptions.Compiled);

    public TransactionProcessorAgent(
        ILogger<TransactionProcessorAgent> logger,
        int boundedCapacity = 100) 
        : base(logger, boundedCapacity)
    {
    }

    protected override async Task<Transaction> ProcessAsync(Transaction transaction)
    {
        _logger.LogDebug("Processing transaction {Id}: {Description}", 
            transaction.Id, transaction.Description);

        try
        {
            var cleanDescription = CleanDescription(transaction.Description);
            var normalizedDate = NormalizeDate(transaction.Date);
            var normalizedAmount = NormalizeAmount(transaction.Amount);

            var processedTransaction = transaction with
            {
                CleanDescription = cleanDescription,
                Date = normalizedDate,
                Amount = normalizedAmount,
                Status = ProcessingStatus.Processed
            };

            _logger.LogDebug("Processed transaction {Id}: '{Original}' -> '{Clean}'", 
                transaction.Id, transaction.Description, cleanDescription);

            return processedTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction {Id}", transaction.Id);
            throw;
        }
    }

    private string CleanDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // Trim and normalize whitespace
        var cleaned = description.Trim();
        cleaned = WhitespaceRegex.Replace(cleaned, " ");

        // Remove excessive special characters while preserving common ones
        cleaned = SpecialCharsRegex.Replace(cleaned, "");

        // Convert to title case for consistency
        cleaned = ConvertToTitleCase(cleaned);

        // Remove redundant words/phrases
        cleaned = RemoveRedundantTerms(cleaned);

        return cleaned;
    }

    private string ConvertToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var titleCased = words.Select(word =>
        {
            // Keep common abbreviations uppercase
            if (IsCommonAbbreviation(word))
                return word.ToUpper();
            
            // Handle mixed case words (like McDonald's)
            if (word.Length > 1 && word.Any(char.IsUpper) && word.Any(char.IsLower))
                return word;
            
            return textInfo.ToTitleCase(word.ToLower());
        });

        return string.Join(" ", titleCased);
    }

    private bool IsCommonAbbreviation(string word)
    {
        var abbreviations = new[] { "ATM", "POS", "ACH", "USD", "API", "LLC", "INC", "CO", "LTD" };
        return abbreviations.Contains(word.ToUpper());
    }

    private string RemoveRedundantTerms(string description)
    {
        var redundantTerms = new[] 
        { 
            "PURCHASE", "PAYMENT", "DEBIT", "CREDIT", "TRANSACTION", 
            "AUTHORIZATION", "PENDING", "PROCESSING", "TEMP", "HOLD"
        };

        var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filtered = words.Where(word => 
            !redundantTerms.Contains(word.ToUpper()) || 
            words.Length <= 2); // Keep if it's one of the few words left

        return string.Join(" ", filtered);
    }

    private DateTime NormalizeDate(DateTime date)
    {
        // Ensure we have a valid date
        if (date == DateTime.MinValue || date == DateTime.MaxValue)
        {
            _logger.LogWarning("Invalid date detected, using current date");
            return DateTime.Today;
        }

        // Normalize to date only (remove time component)
        return date.Date;
    }

    private decimal NormalizeAmount(decimal amount)
    {
        // Ensure positive amount and round to 2 decimal places
        var normalizedAmount = Math.Abs(amount);
        return Math.Round(normalizedAmount, 2, MidpointRounding.AwayFromZero);
    }
}