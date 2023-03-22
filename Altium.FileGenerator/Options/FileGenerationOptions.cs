using FluentValidation;

namespace Altium.FileGenerator.Options;

internal sealed class FileGenerationOptions
{
    public const string Section = "FileGeneration";
    public int SizeInMB { get; set; }
    public string FilePath { get; set; }
}

internal sealed class FileGenerationOptionsValidator : AbstractValidator<FileGenerationOptions>
{
    public FileGenerationOptionsValidator()
    {
        RuleFor(n => n.FilePath).NotEmpty();
        RuleFor(n => n.SizeInMB).GreaterThan(0);
    }
}
