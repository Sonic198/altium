using Altium.FileSorter.Options;
using Altium.Shared.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

internal sealed class ExternalSortService : IExternalSortService
{
    private readonly ExternalSortOptions _settings;
    private readonly ILogger<ExternalSortService> _logger;    

    private const string _unsortedFileExtension = ".unsorted";
    private const string _sortedFileExtension = ".sorted";
    private const string _tempFileExtension = ".tmp";

    private Row[] _unsortedRows = Array.Empty<Row>();
    private double _totalFilesToMerge;
    private int _mergeFilesProcessed;

    public ExternalSortService(
		IOptions<ExternalSortOptions> settings,
		ILogger<ExternalSortService> logger)
	{
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SortAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Start file sorting");
        var sw = Stopwatch.StartNew();

        var files = await SplitFileAsync(_settings.SourceFilePath);
        _unsortedRows = new Row[_settings.SplitFile.RowsPerFile];

        var target = File.Create(_settings.TargetFilePath);

        if (files.Count == 1)
        {
            var unsortedFilePath = Path.Combine(_settings.TempFilesLocation, files.First());
            await SortFile(File.OpenRead(unsortedFilePath), target);
            File.Delete(unsortedFilePath);
            return;
        }

        var sortedFiles = await SortFilesAsync(files);

        _totalFilesToMerge = CalculateTotalFilesToMerge(sortedFiles, _settings.MergeFile.FilesPerRun);

        await MergeFilesAsync(sortedFiles, target, cancellationToken);

        sw.Stop();

        _logger.LogInformation("End file sorting: {ExecutionTime}", sw.Elapsed);
    }

    private static double CalculateTotalFilesToMerge(IReadOnlyList<string> sortedFiles, int size)
    {
        var filesToMerge = sortedFiles.Count;
        var result = sortedFiles.Count / size;
        var done = false;
        while (!done)
        {
            if (result <= 0)
            {
                done = true;
            }
            filesToMerge += result;
            result /= size;
        }

        return filesToMerge;
    }

    private async Task<IReadOnlyCollection<string>> SplitFileAsync(string sourceFilePath)
    {
        var linesToRead = 100_000;
        var filenames = new List<string>();

        using (var sr = new StreamReader(sourceFilePath, new FileStreamOptions { BufferSize = _settings.SortFile.InputBufferSize }))
        {
            if (sr.BaseStream.Length == 0)
                throw new ApplicationException("File can't be empty");

            var linesCount = 0;
            var currentFile = 0;
            string line;

            var filename = $"{++currentFile}{_unsortedFileExtension}";
            var fileOptions = new FileStreamOptions { BufferSize = _settings.SortFile.OutputBufferSize, Access = FileAccess.Write, Mode = FileMode.Create };
            var streamWriter = new StreamWriter(Path.Combine(_settings.TempFilesLocation, filename), fileOptions);

            while ((line = sr.ReadLine()) != null)
            {
                streamWriter.WriteLine(line);

                linesCount++;

                if (linesToRead == linesCount)
                {
                    linesCount = 0;
                    await streamWriter.DisposeAsync();
                    filenames.Add(filename);

                    filename = $"{++currentFile}{_unsortedFileExtension}";
                    streamWriter = new StreamWriter(Path.Combine(_settings.TempFilesLocation, filename), fileOptions);
                    ReportSplitProgress((double)sr.BaseStream.Position / sr.BaseStream.Length);
                }
            }
            await streamWriter.DisposeAsync();
            filenames.Add(filename);
            ReportSplitProgress((double)sr.BaseStream.Position / sr.BaseStream.Length);
        }
        return filenames;
    }

    private async Task<IReadOnlyList<string>> SortFilesAsync(IReadOnlyCollection<string> unsortedFiles)
    {
        var sortedFiles = new List<string>(unsortedFiles.Count);
        double totalFiles = unsortedFiles.Count;
        foreach (var unsortedFile in unsortedFiles)
        {
            var sortedFilename = unsortedFile.Replace(_unsortedFileExtension, _sortedFileExtension);
            var unsortedFilePath = Path.Combine(_settings.TempFilesLocation, unsortedFile);
            var sortedFilePath = Path.Combine(_settings.TempFilesLocation, sortedFilename);

            await SortFile(File.OpenRead(unsortedFilePath), File.OpenWrite(sortedFilePath));
            File.Delete(unsortedFilePath);
            sortedFiles.Add(sortedFilename);
            ReportSortProgress(sortedFiles.Count / totalFiles);
        }
        return sortedFiles;
    }

    private async Task SortFile(Stream unsortedFile, Stream target)
    {
        using var streamReader = new StreamReader(unsortedFile, bufferSize: _settings.SortFile.InputBufferSize);

        var counter = 0;
        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync();
            _unsortedRows[counter++] = line.Parse();
        }

        Array.Sort(_unsortedRows);
        using var streamWriter = new StreamWriter(target, bufferSize: _settings.SortFile.OutputBufferSize);

        foreach (var row in _unsortedRows.Where(n => n is not null))
        {
            await streamWriter.WriteLineAsync($"{row.Number}. {row.Text}");
        }

        Array.Clear(_unsortedRows, 0, _unsortedRows.Length);
    }

    private async Task MergeFilesAsync(
        IReadOnlyList<string> sortedFiles, Stream target, CancellationToken cancellationToken)
    {
        var done = false;
        while (!done)
        {
            var runSize = _settings.MergeFile.FilesPerRun;
            var finalRun = sortedFiles.Count <= runSize;

            if (finalRun)
            {
                await Merge(sortedFiles, target, cancellationToken);
                return;
            }

            var runs = sortedFiles.Chunk(runSize);
            var chunkCounter = 0;
            foreach (var files in runs)
            {
                var outputFilename = $"{++chunkCounter}{_sortedFileExtension}{_tempFileExtension}";
                if (files.Length == 1)
                {
                    OverwriteTempFile(files.First(), outputFilename);
                    continue;
                }

                var outputStream = File.OpenWrite(TempFileFullPath(outputFilename));
                await Merge(files, outputStream, cancellationToken);
                OverwriteTempFile(outputFilename, outputFilename);

                void OverwriteTempFile(string from, string to)
                {
                    File.Move(
                        TempFileFullPath(from),
                        TempFileFullPath(to.Replace(_tempFileExtension, string.Empty)), true);
                }
            }

            sortedFiles = Directory.GetFiles(_settings.TempFilesLocation, $"*{_sortedFileExtension}")
                .OrderBy(x =>
                {
                    var filename = Path.GetFileNameWithoutExtension(x);
                    return int.Parse(filename);
                })
                .ToArray();

            if (sortedFiles.Count > 1)
            {
                continue;
            }

            done = true;
        }
    }

    private async Task Merge(
        IReadOnlyList<string> filesToMerge,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        var (streamReaders, rows) = await InitializeStreamReaders(filesToMerge);
        var finishedStreamReaders = new List<int>(streamReaders.Length);
        var done = false;
        await using var outputWriter = new StreamWriter(outputStream, bufferSize: _settings.MergeFile.OutputBufferSize);

        while (!done)
        {
            rows.Sort();
            var valueToWrite = $"{rows[0].Number}. {rows[0].Text}";
            var streamReaderIndex = rows[0].StreamReader;
            await outputWriter.WriteLineAsync(valueToWrite.AsMemory(), cancellationToken);

            if (streamReaders[streamReaderIndex].EndOfStream)
            {
                var indexToRemove = rows.FindIndex(x => x.StreamReader == streamReaderIndex);
                rows.RemoveAt(indexToRemove);
                finishedStreamReaders.Add(streamReaderIndex);
                done = finishedStreamReaders.Count == streamReaders.Length;
                ReportMergeProgress(++_mergeFilesProcessed / _totalFilesToMerge);
                continue;
            }

            var value = await streamReaders[streamReaderIndex].ReadLineAsync(cancellationToken);
            rows[0] = value.Parse(streamReaderIndex);
        }

        CleanupRun(streamReaders, filesToMerge);
    }

    private async Task<(StreamReader[] StreamReaders, List<Row> rows)> InitializeStreamReaders(
        IReadOnlyList<string> sortedFiles)
    {
        var streamReaders = new StreamReader[sortedFiles.Count];
        var rows = new List<Row>(sortedFiles.Count);
        for (var i = 0; i < sortedFiles.Count; i++)
        {
            var sortedFilePath = TempFileFullPath(sortedFiles[i]);
            var sortedFileStream = File.OpenRead(sortedFilePath);
            streamReaders[i] = new StreamReader(sortedFileStream, bufferSize: _settings.MergeFile.InputBufferSize);
            var value = await streamReaders[i].ReadLineAsync();
            var row = value.Parse(i);
            rows.Add(row);
        }

        return (streamReaders, rows);
    }

    private void CleanupRun(StreamReader[] streamReaders, IReadOnlyList<string> filesToMerge)
    {
        for (var i = 0; i < streamReaders.Length; i++)
        {
            streamReaders[i].Dispose();
            // RENAME BEFORE DELETION SINCE DELETION OF LARGE FILES CAN TAKE SOME TIME
            // WE DON'T WANT TO CLASH WHEN WRITING NEW FILES.
            var temporaryFilename = $"{filesToMerge[i]}.removal";
            File.Move(TempFileFullPath(filesToMerge[i]), TempFileFullPath(temporaryFilename));
            File.Delete(TempFileFullPath(temporaryFilename));
        }
    }

    private string TempFileFullPath(string filename)
    {
        return Path.Combine(_settings.TempFilesLocation, Path.GetFileName(filename));
    }

    private void ReportSplitProgress(double value)
    {
        _logger.LogInformation("Split progress: {Percentage}%", value.ToString("P"));
    }

    private void ReportSortProgress(double value)
    {
        _logger.LogInformation("Sort progress: {Percentage}%", value.ToString("P"));
    }

    private void ReportMergeProgress(double value)
    {
        _logger.LogInformation(message: "Merge progress: {Percentage}%", value.ToString("P"));
    }
}