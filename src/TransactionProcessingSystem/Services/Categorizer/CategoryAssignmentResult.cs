namespace TransactionProcessingSystem.Services.Categorizer;

public record CategoryAssignmentResult
{
    public bool IsValid { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    private CategoryAssignmentResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static CategoryAssignmentResult Success() => new(true);

    public static CategoryAssignmentResult Failure(string errorMessage) => new(false, errorMessage);
}
