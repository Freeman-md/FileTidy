using System;
using System.Collections.Generic;
using FileTidy.Core.Interfaces;

namespace FileTidy.GUI.Reporting;

public class GuiFileSortReporter : ISortReporter
{
    private readonly Action<int>? _progressUpdate;
    private readonly Action<string>? _elapsedUpdate;
    private readonly Action<int>? _filesProcessedUpdate;
    private readonly Action<string, string>? _notify;

    private int _total = 0;
    private int _processed = 0;

    public GuiFileSortReporter(
        Action<int>? progressUpdate, 
        Action<string>? elapsedUpdate, 
        Action<int>? filesProcessedUpdate,
        Action<string, string>? notify = null
        )
    {
        _progressUpdate = progressUpdate;
        _elapsedUpdate = elapsedUpdate;
        _filesProcessedUpdate = filesProcessedUpdate;
        _notify = notify;
    }

    public void SetTotalFiles(int total)
    {
        _total = total;
        _processed = 0;
        _progressUpdate?.Invoke(0);
    }

    public void OnElapsedTimeReported(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 1)
            _elapsedUpdate?.Invoke($"{elapsed.TotalMilliseconds:F0}ms");
        else
            _elapsedUpdate?.Invoke($"{(int)elapsed.TotalMinutes}m {elapsed.Seconds:D2}s");
    }

    public void OnFileReverted(string operationNewPath)
    {
        _processed++;
        var progress = (_total > 0) ? (_processed * 100) / _total : 0;
        _progressUpdate?.Invoke(progress);
        _filesProcessedUpdate?.Invoke(_processed);
    }

    public void OnSessionReverted(Guid sessionId)
    {
        _notify?.Invoke("Session Reverted", $"All sorted files in last session have been reverted.");
    }


    public void OnBulkRevertSummary(int total, int reverted, int failed)
    {
        _notify?.Invoke(
            "Revert Summary",
            $"Reverted {reverted} of {total} files. {failed} failed."
        );
    }


    public void OnFileDeleted(string path)
    {
        _notify?.Invoke("File Deleted", $"{System.IO.Path.GetFileName(path)} was permanently deleted.");
    }


    public void OnBulkDeleteSummary(int total, int deleted, int failed)
    {
        _notify?.Invoke(
            "Deletion Summary",
            $"Deleted {deleted} of {total} files. {failed} failed."
        );
    }


    public void OnFileProcessed(string filePath, string category)
    {
        _processed++;
        var progress = (_total > 0) ? (_processed * 100) / _total : 0;
        _progressUpdate?.Invoke(progress);
        _filesProcessedUpdate?.Invoke(_processed);
    }

    public void OnFileSkipped(string filePath)
    {
        _notify?.Invoke("File Skipped", $"{System.IO.Path.GetFileName(filePath)} was skipped during sorting.");
    }


    public void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts)
    {
        _filesProcessedUpdate?.Invoke(totalMoved);
        
        string message = $"Sorted {totalMoved} out of {totalFiles} files";
        if (totalErrors > 0)
            message += $", with {totalErrors} errors.";

        _notify?.Invoke("Sorting Complete", message);
    }

    public void OnError(string filePath, Exception ex)
    {
        _notify?.Invoke("Error Occurred", $"Issue with {System.IO.Path.GetFileName(filePath)}: {ex.Message}");
    }


    public void OnDirectoryEmptied(string directoryPath)
    {
        string folderName = System.IO.Path.GetFileName(directoryPath);
        
        // do nothing for now
        // _notify?.Invoke("Folder Removed", $"The folder '{folderName}' was empty and has been deleted.");
        
    }

}
