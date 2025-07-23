using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Validator for Pipeline settings with performance constraints
/// </summary>
public sealed class PipelineSettingsValidator : IValidateOptions<PipelineSettings>
{
    public ValidateOptionsResult Validate(string? name, PipelineSettings options)
    {
        if (options.MaxDegreeOfParallelism > Environment.ProcessorCount)
        {
            ValidateOptionsResult.Fail($"Pipeline.MaxDegreeOfParallelism must not exceed processor count {Environment.ProcessorCount}");
        }

        return ValidateOptionsResult.Success;
    }
}