namespace Altium.FileGenerator.Services.Interface;

internal interface IFileGeneratorService
{
    Task GenerateFile(CancellationToken cancellationToken = default);
}
