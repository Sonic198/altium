using FluentValidation;

namespace Altium.FileSorter.Options;

internal sealed class SplitFileOptions
{
    public int RowsPerFile { get; set; }
}


internal sealed class SplitFileOptionsValidator : AbstractValidator<SplitFileOptions>
{
    public SplitFileOptionsValidator()
    {
        RuleFor(n => n.RowsPerFile).GreaterThan(0);
    }
}
