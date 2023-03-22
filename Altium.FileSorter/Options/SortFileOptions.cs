using FluentValidation;

namespace Altium.FileSorter.Options;

internal sealed class SortFileOptions
{
    public int InputBufferSize { get; set; }
    public int OutputBufferSize { get; set; }
}

internal sealed class SortFileOptionsValidator : AbstractValidator<SortFileOptions>
{
    public SortFileOptionsValidator()
    {
        RuleFor(n => n.InputBufferSize).GreaterThan(0);
        RuleFor(n => n.OutputBufferSize).GreaterThan(0);
    }
}