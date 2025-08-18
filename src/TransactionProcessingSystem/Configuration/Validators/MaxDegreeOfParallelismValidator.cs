using Microsoft.Extensions.Options;

namespace TransactionProcessingSystem.Configuration.Validators;

/// <summary>
/// Validator for Max Degree Of Parallelism settings.
/// </summary>
public sealed class MaxDegreeOfParallelismValidator : IValidateOptions<PipelineOptions>
{
    public ValidateOptionsResult Validate(string? name, PipelineOptions options)
    {
        if (options.MaxDegreeOfParallelism > Environment.ProcessorCount)
        {
            ValidateOptionsResult.Fail($"Pipeline.MaxDegreeOfParallelism must not exceed processor count {Environment.ProcessorCount}");
        }

        return ValidateOptionsResult.Success;
    }
}