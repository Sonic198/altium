using Altium.FileGenerator.Options;
using Altium.FileGenerator.Services.Interface;
using Altium.Shared.Dtos;
using Bogus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Altium.FileGenerator.Services;

internal sealed class FileGeneratorService : IFileGeneratorService
{
    private readonly IOptions<FileGenerationOptions> _settings;
    private readonly ILogger<FileGeneratorService> _logger;
    private readonly Faker<Row> _objectGenerator;

    public FileGeneratorService(
        IOptions<FileGenerationOptions> settings,
        ILogger<FileGeneratorService> logger)
	{
        _settings = settings;
        _logger = logger;
        _objectGenerator = new Faker<Row>()
            .RuleFor(fake => fake.Number, fake => fake.Random.UInt())
            .RuleFor(fake => fake.Text, fake => fake.Random.Words(Random.Shared.Next(1, 6)));
    }

    public async Task GenerateFile(CancellationToken cancellationToken = default)
    {
        var filePath = _settings.Value.FilePath;

        File.Delete(filePath);

        _logger.LogInformation("Start file generation: {FileName}", filePath);
        var sw = Stopwatch.StartNew();

        long fileSize = (long)1024 * (long)1024 * (long)_settings.Value.SizeInMB;

        //some of them can be moved to appsettings.json
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None, BufferSize = 81920, PreallocationSize = fileSize };

        await using (var stream = new FileStream(filePath, options))        
        {
            var newLine = Encoding.Default.GetBytes(Environment.NewLine);

            while (stream.Length < fileSize)
            {
                var data = _objectGenerator.Generate();               

                var line = Encoding.Default.GetBytes($"{data.Number}. {data.Text}");

                //var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref zz, 1));

                //var sp = new  Span<byte>(zz.Length);

                //var line = Encoding.Default.GetBytes($"{data.Number}. {data.Text}{Environment.NewLine}".AsSpan(), sp);
                //var line = MemoryMarshal.Cast<char, byte>(zz);

                //stream.Write(sp);

                await stream.WriteAsync(line, cancellationToken);
                await stream.WriteAsync(newLine, cancellationToken);
            } 
        }

        sw.Stop();

        _logger.LogInformation("End file generation: {FileName}, {ExecutionTime}", filePath, sw.Elapsed);
    }
}
