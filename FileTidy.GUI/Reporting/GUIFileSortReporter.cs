using System;
using System.Collections.Generic;
using FileTidy.Core.Interfaces;

namespace FileTidy.GUI.Reporting;

public class GuiFileSortReporter : ISortReporter
{
    public event Action<int>? ProgressChanged;
    public event Action<string>? ElapsedChanged;
    public event Action<int>? FilesProcessedChanged;
    public event Action<string, string>? NotificationRequested;

    private int _total = 0;
    private int _processed = 0;

    public GuiFileSortReporter() { }

    public void SetTotalFiles(int total)
    {
        _total = total;
        _processed = 0;
        ProgressChanged?.Invoke(0);
    }

    public void OnElapsedTimeReported(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 1)
            ElapsedChanged?.Invoke($"{elapsed.TotalMilliseconds:F0}ms");
        else
            ElapsedChanged?.Invoke($"{(int)elapsed.TotalMinutes}m {elapsed.Seconds:D2}s");
    }

    public void OnFileReverted(string operationNewPath)
    {
        _processed++;
        var progress = (_total > 0) ? (_processed * 100) / _total : 0;
        ProgressChanged?.Invoke(progress);
        FilesProcessedChanged?.Invoke(_processed);
    }

    public void OnSessionReverted(Guid sessionId)
    {
        NotificationRequested?.Invoke("Session Reverted", $"All sorted files in last session have been reverted.");
    }

    public void OnBulkRevertSummary(int total, int reverted, int failed)
    {
        NotificationRequested?.Invoke(
            "Revert Summary",
            $"Reverted {reverted} of {total} files. {failed} failed."
        );
    }

    public void OnFileDeleted(string path)
    {
        NotificationRequested?.Invoke("File Deleted", $"{System.IO.Path.GetFileName(path)} was permanently deleted.");
    }

    public void OnBulkDeleteSummary(int total, int deleted, int failed)
    {
        NotificationRequested?.Invoke(
            "Deletion Summary",
            $"Deleted {deleted} of {total} files. {failed} failed."
        );
    }

    public void OnFileProcessed(string filePath, string category)
    {
        _processed++;
        var progress = (_total > 0) ? (_processed * 100) / _total : 0;
        ProgressChanged?.Invoke(progress);
        FilesProcessedChanged?.Invoke(_processed);
    }

    public void OnFileSkipped(string filePath)
    {
        NotificationRequested?.Invoke("File Skipped", $"{System.IO.Path.GetFileName(filePath)} was skipped during sorting.");
    }

    public void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts)
    {
        FilesProcessedChanged?.Invoke(totalMoved);
        string message = $"Sorted {totalMoved} out of {totalFiles} files";
        if (totalErrors > 0)
            message += $", with {totalErrors} errors.";
        NotificationRequested?.Invoke("Sorting Complete", message);
    }

    public void OnError(string filePath, Exception ex)
    {
        NotificationRequested?.Invoke("Error Occurred", $"Issue with {System.IO.Path.GetFileName(filePath)}: {ex.Message}");
    }

    public void OnDirectoryEmptied(string directoryPath)
    {
        string folderName = System.IO.Path.GetFileName(directoryPath);
        // do nothing for now
        // NotificationRequested?.Invoke("Folder Removed", $"The folder '{folderName}' was empty and has been deleted.");
    }
}
