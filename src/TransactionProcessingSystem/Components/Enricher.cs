using System.Text;
using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Components;

public class Enricher(MicrosoftGraphService graphService)
{
    public async Task<string> EnrichAsync(DateTime from, DateTime to)
    {
        var emails = await graphService.GetEmailsAsync(from, to);
        if (emails == null || emails.Value == null || emails.Value.Count == 0)
        {
            return "No emails found in the specified date range.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Recent Emails:");
        foreach (var email in emails.Value)
        {
            sb.AppendLine(FormattableString.Invariant($"- Subject: {email.Subject}, From: {email.From?.EmailAddress?.Name}, Received: {email.ReceivedDateTime}"));
            sb.AppendLine(FormattableString.Invariant($"  Preview: {email.BodyPreview}"));
        }

        return sb.ToString();
    }
}
