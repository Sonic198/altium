using FluentValidation;

namespace Altium.FileSorter.Options;

internal sealed partial class ExternalSortOptions
{
    public const string Section = "ExternalSort";

    public string SourceFilePath { get; set; }
    public string TargetFilePath { get; set; }
    public string TempFilesLocation { get; set; }    
    public SplitFileOptions SplitFile { get; set; }
    public SortFileOptions SortFile { get; set; }
    public MergeFileOptions MergeFile { get; set; }
}

internal sealed class ExternalSortOptionsValidator : AbstractValidator<ExternalSortOptions>
{
	public ExternalSortOptionsValidator(
        IValidator<SplitFileOptions> splitOptionsValidator,
        IValidator<SortFileOptions> sortOptionsValidator,
        IValidator<MergeFileOptions> mergeOptionsValidator)
	{
        RuleFor(n => n.SourceFilePath).NotEmpty();
        RuleFor(n => n.TargetFilePath).NotEmpty();
        RuleFor(n => n.TempFilesLocation).NotEmpty();

        RuleFor(n => n.SplitFile).NotNull().SetValidator(splitOptionsValidator);
        RuleFor(n => n.SortFile).NotNull().SetValidator(sortOptionsValidator);
        RuleFor(n => n.MergeFile).NotNull().SetValidator(mergeOptionsValidator);
    }
}
