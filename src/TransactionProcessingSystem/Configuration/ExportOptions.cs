using System.ComponentModel.DataAnnotations;

namespace TransactionProcessingSystem.Configuration;

/// <summary>
/// Export configuration settings.
/// </summary>
public record ExportOptions
{
    [Required]
    [RegularExpression(@"^[^<>:""/\\|?*\r\n]+([\\/][^<>:""/\\|?*\r\n]+)*$", ErrorMessage = "OutputDirectory must be a valid path (absolute or relative).")]
    public required string OutputDirectory { get; init; }

    [Required]
    [RegularExpression(@".*\{0\}.*", ErrorMessage = "FileNameFormat must contain the {0} placeholder for timestamp.")]
    public required string FileNameFormat { get; init; }

    [Required]
    [Range(1, 1024 * 1024)]
    public required int BufferSize { get; init; }
}
