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

        if (retrievedFileOperation is null)
        {
            _reporter?.OnError(newPath,
                new InvalidOperationException($"Cannot revert. No active operation found for file at path: {newPath}"));

            return;
        }


        await _fileManager.RevertFileAsync(retrievedFileOperation.NewPath, retrievedFileOperation.OriginalPath,
            cancellationToken);

        await _fileOperationStore.UpdateOperationStatusAsync(retrievedFileOperation.Id, FileOperationStatus.Reverted);
    }
    
    public async Task DeleteFileAsync(string path,
        CancellationToken cancellationToken = default)
    {
        await _fileManager.DeleteFileAsync(path, cancellationToken);
        
        //TODO: Add File Operation to get file operation by path. Either original path or new path. And then update the file operation status if an operation exists
    }

    public async Task RevertSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var operations = await _fileOperationStore.GetOperationsBySessionAsync(sessionId);

        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _fileManager.RevertFileAsync(operation.NewPath, operation.OriginalPath, cancellationToken);
                await _fileOperationStore.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Reverted);
                _reporter?.OnFileReverted(operation.NewPath);
            }
            catch (Exception ex)
            {
                _reporter?.OnError(operation.NewPath, ex);
            }
        }

        _reporter?.OnSessionReverted(sessionId);
    }
    
    public async Task RevertFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        int total = 0, reverted = 0, failed = 0;

        foreach (var path in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total++;

            try
            {
                var operation = await _fileOperationStore
                    .GetLatestNonRevertedOperationByNewPathAsync(path, FileOperationStatus.Moved);

                if (operation == null)
                {
                    _reporter?.OnError(path, new InvalidOperationException(
                        $"Cannot revert. No active operation found for file at path: {path}"));
                    failed++;
                    continue;
                }

                await _fileManager.RevertFileAsync(operation.NewPath, operation.OriginalPath, cancellationToken);
                await _fileOperationStore.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Reverted);
                _reporter?.OnFileReverted(operation.NewPath);
                reverted++;
            }
            catch (Exception ex)
            {
                _reporter?.OnError(path, ex);
                failed++;
            }
        }

        _reporter?.OnBulkRevertSummary(total, reverted, failed);
    }
    
    public async Task DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        int total = 0, deleted = 0, failed = 0;

        foreach (var path in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total++;

            try
            {
                await _fileManager.DeleteFileAsync(path, cancellationToken);

                //TODO: Add File Operation to get file operation by path. Either original path or new path. And then update the file operation status if an operation exists

                _reporter?.OnFileDeleted(path);
                deleted++;
            }
            catch (Exception ex)
            {
                _reporter?.OnError(path, ex);
                failed++;
            }
        }

        _reporter?.OnBulkDeleteSummary(total, deleted, failed);
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