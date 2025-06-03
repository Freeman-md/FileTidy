using System.Diagnostics;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using FileTidy.Core.Utils;

namespace FileTidy.Core.Services;

public class FileTidyingService : IFileTidyingService
{
    private readonly IFileOperationStore _fileOperationStore;
    private readonly FileCategoryMapper _mapper;
    private readonly ISortReporter? _reporter;
    private readonly FileManager _fileManager = new();

    public FileTidyingService(IFileOperationStore fileOperationStore, ISortReporter? reporter = null,
        string? dataPath = null)
    {
        _fileOperationStore = fileOperationStore;
        _reporter = reporter;

        string resolvedPath = dataPath ??
                              Path.Combine(Path.GetDirectoryName(typeof(FileTidyingService).Assembly.Location)!,
                                  "Data");
        _mapper = new FileCategoryMapper(resolvedPath);
    }

    public async Task<TidyingResult> SortDirectory(string directoryPath, CancellationToken cancellationToken = default)
    {
        var filesToProcess = GetFilesToProcess(directoryPath);
        if (!filesToProcess.Any())
        {
            return new TidyingResult
            {
                TotalFiles = 0,
                TotalMoved = 0,
                TotalErrors = 0,
                PerCategoryCounts = new(),
                Elapsed = TimeSpan.Zero
            };
        }

        _reporter?.SetTotalFiles(filesToProcess.Count);
        Stopwatch stopwatch = Stopwatch.StartNew();

        Dictionary<string, int> perCategoryCounts = new();
        int totalMoved = 0, totalErrors = 0, processed = 0;
        Guid sortSessionId = Guid.NewGuid();

        foreach (var file in filesToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string ext = Path.GetExtension(file);
            string category = _mapper.GetCategory(ext);

            var result = await _fileManager.MoveFileAsync(file, category, directoryPath, cancellationToken);

            if (result.Success)
            {
                perCategoryCounts[category] = perCategoryCounts.GetValueOrDefault(category, 0) + 1;
                totalMoved++;

                await _fileOperationStore.LogOperationAsync(new FileOperation
                {
                    Id = Guid.NewGuid(),
                    FileName = Path.GetFileName(file),
                    OriginalPath = result.OriginalPath,
                    NewPath = result.NewPath,
                    Status = result.Status,
                    Timestamp = DateTime.UtcNow,
                    SortSessionId = sortSessionId
                });

                _reporter?.OnFileProcessed(file, category);
            }
            else if (result.Status == FileOperationStatus.Skipped)
            {
                _reporter?.OnFileSkipped(file);
            }
            else
            {
                totalErrors++;
                _reporter?.OnError(file, result.Error!);
            }

            processed++;
            if (processed % 10 == 0)
                _reporter?.OnElapsedTimeReported(stopwatch.Elapsed);
        }

        stopwatch.Stop();
        _reporter?.OnElapsedTimeReported(stopwatch.Elapsed);
        _reporter?.OnSummary(filesToProcess.Count, totalMoved, totalErrors, perCategoryCounts);

        RemoveEmptyDirectories(directoryPath);

        return new TidyingResult
        {
            TotalFiles = filesToProcess.Count,
            TotalMoved = totalMoved,
            TotalErrors = totalErrors,
            PerCategoryCounts = perCategoryCounts,
            Elapsed = stopwatch.Elapsed
        };
    }

    public async Task RevertFileAsync(string newPath,
        CancellationToken cancellationToken = default)
    {
        var retrievedFileOperation =
            await _fileOperationStore.GetLatestNonRevertedOperationByNewPathAsync(newPath, FileOperationStatus.Moved);

        if (retrievedFileOperation == null)
        {
            _reporter?.OnError(newPath,
                new InvalidOperationException($"Cannot revert. No active operation found for file at path: {newPath}"));

            return;
        }


        await _fileManager.RevertFileAsync(retrievedFileOperation.NewPath, retrievedFileOperation.OriginalPath,
            cancellationToken);

        await _fileOperationStore.UpdateOperationStatusAsync(retrievedFileOperation.Id, FileOperationStatus.Reverted);
    }

    private void RemoveEmptyDirectories(string directory)
    {
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            RemoveEmptyDirectories(subDirectory);

            if (!Directory.EnumerateFileSystemEntries(subDirectory).Any())
            {
                try
                {
                    Directory.Delete(subDirectory);
                    _reporter?.OnDirectoryEmptied(subDirectory);
                }
                catch (Exception ex)
                {
                    _reporter?.OnError(subDirectory, ex);
                }
            }
        }
    }


    private List<string> GetFilesToProcess(string directoryPath)
    {
        var allFiles = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();

        var categoryFolders = _mapper.GetAllCategoryNames()
            .Select(c => Path.Combine(directoryPath, c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allFiles
            .Where(file =>
                !categoryFolders.Any(folder =>
                    file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}