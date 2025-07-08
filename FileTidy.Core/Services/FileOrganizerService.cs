using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FileTidy.Core.Utils;

namespace FileTidy.Core.Services;

public class FileOrganizerService : IFileOrganizerService
{
    private readonly IFileOperationStore _fileOperationStore;
    private readonly IFileCategoryService _fileCategoryService;
    private readonly ISortReporter? _reporter;
    private readonly IFileOperationService _fileOperationService;

    public FileOrganizerService(
        IFileOperationStore fileOperationStore,
        IFileCategoryService fileCategoryService,
        IFileOperationService fileOperationService,
        ISortReporter? reporter = null
        )
    {
        _fileOperationStore = fileOperationStore;
        _fileCategoryService = fileCategoryService;
        _reporter = reporter;
        _fileOperationService = fileOperationService;
    }
    
    public async Task<TidyingResult> SortDirectoryAsync(string directoryPath, Guid sortSessionId,
        CancellationToken cancellationToken = default)
    {
        var filesToProcess = await GetFilesToProcess(directoryPath);
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

        foreach (var file in filesToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string ext = Path.GetExtension(file);
            string category = _fileCategoryService.GetCategory(ext);

            bool success = await TryMoveAndLogFileAsync(file, category, directoryPath, sortSessionId, cancellationToken);

            if (success)
            {
                perCategoryCounts[category] = perCategoryCounts.GetValueOrDefault(category, 0) + 1;
                totalMoved++;
            }
            else
            {
                totalErrors++;
            }

            processed++;
            if (processed % 10 == 0)
                _reporter?.OnElapsedTimeReported(stopwatch.Elapsed);
        }

        stopwatch.Stop();
        _reporter?.OnElapsedTimeReported(stopwatch.Elapsed);
        _reporter?.OnSummary(filesToProcess.Count, totalMoved, totalErrors, perCategoryCounts);

        _ = _fileOperationService.RemoveEmptyDirectoriesAsync(directoryPath);

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


        await _fileOperationService.RevertFileAsync(retrievedFileOperation.NewPath, retrievedFileOperation.OriginalPath,
            cancellationToken);

        await _fileOperationStore.UpdateOperationStatusAsync(retrievedFileOperation.Id, FileOperationStatus.Reverted);
    }

    public async Task DeleteFileAsync(string path,
        CancellationToken cancellationToken = default)
    {
        await _fileOperationService.DeleteFileAsync(path, cancellationToken);

        //TODO: Add File Operation to get file operation by path. Either original path or new path. And then update the file operation status if an operation exists
    }

    public async Task RevertSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var operations = (await _fileOperationStore.GetOperationsBySessionAsync(sessionId)).ToList();

        if (!operations.Any())
        {
            _reporter?.OnBulkRevertSummary(0, 0, 0);
            return;
        }

        _reporter?.SetTotalFiles(operations.Count);
        var stopwatch = Stopwatch.StartNew();

        int successCount = 0;
        int failureCount = 0;

        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _fileOperationService.RevertFileAsync(operation.NewPath, operation.OriginalPath, cancellationToken);
                await _fileOperationStore.UpdateOperationStatusAsync(operation.Id, FileOperationStatus.Reverted);

                successCount++;
                _reporter?.OnFileReverted(operation.NewPath);

                if (successCount % 10 == 0)
                    _reporter?.OnElapsedTimeReported(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                failureCount++;
                _reporter?.OnError(operation.NewPath, ex);
            }
        }

        stopwatch.Stop();
        _reporter?.OnElapsedTimeReported(stopwatch.Elapsed);
        _reporter?.OnBulkRevertSummary(operations.Count, successCount, failureCount);
        _reporter?.OnSessionReverted(sessionId);
        
        var baseDir = Path.GetDirectoryName(operations.First().OriginalPath);
        if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
            _fileOperationService.RemoveEmptyDirectoriesAsync(baseDir);
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

                await _fileOperationService.RevertFileAsync(operation.NewPath, operation.OriginalPath, cancellationToken);
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
                await _fileOperationService.DeleteFileAsync(path, cancellationToken);

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
    
    private async Task<List<string>> GetFilesToProcess(string directoryPath)
    {
        var allFiles = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
        
        var categoryFolders = _fileCategoryService.GetAllCategoryNames()
            .Select(category => Path.Combine(directoryPath, category))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allFiles
            .Where(file =>
                !categoryFolders.Any(folder =>
                    file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private async Task<bool> TryMoveAndLogFileAsync(
        string file,
        string category,
        string baseDirectory,
        Guid sortSessionId,
        CancellationToken cancellationToken)
    {
        var result = await _fileOperationService.MoveFileAsync(file, category, baseDirectory, cancellationToken);

        if (!result.Success)
        {
            if (result.Status == FileOperationStatus.Skipped)
                _reporter?.OnFileSkipped(file);
            else
                _reporter?.OnError(file, result.Error!);

            return false;
        }

        try
        {
            await RetryHelper.RetryAsync(() => _fileOperationStore.LogOperationAsync(new FileOperation
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(file),
                OriginalPath = result.OriginalPath,
                NewPath = result.NewPath,
                Status = result.Status,
                Timestamp = DateTime.UtcNow,
                SortSessionId = sortSessionId
            }));

            _reporter?.OnFileProcessed(file, category);
            return true;
        }
        catch (Exception ex)
        {
            // Rollback file move
            await _fileOperationService.RevertFileAsync(result.NewPath, result.OriginalPath, cancellationToken);

            _reporter?.OnError(file, ex);
            return false;
        }
    }
} 