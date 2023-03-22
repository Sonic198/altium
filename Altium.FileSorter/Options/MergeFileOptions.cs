using FluentValidation;

namespace Altium.FileSorter.Options;

internal sealed class MergeFileOptions
{
    public int FilesPerRun { get; init; }
    public int InputBufferSize { get; set; }
    public int OutputBufferSize { get; set; }
}

internal sealed class MergeFileOptionsValidator : AbstractValidator<MergeFileOptions>
{
    public MergeFileOptionsValidator()
    {
        RuleFor(n => n.FilesPerRun).GreaterThan(0);
        RuleFor(n => n.InputBufferSize).GreaterThan(0);
        RuleFor(n => n.OutputBufferSize).GreaterThan(0);
    }
}