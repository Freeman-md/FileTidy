using System;
using System.Collections.Generic;
using FileTidy.Core.Interfaces;

namespace FileTidy.GUI.Reporting;

public class GuiFileSortReporter : ISortReporter
{
    private readonly Action<int>? _progressUpdate;
    private readonly Action<string>? _elapsedUpdate;
    private readonly Action<int>? _filesMovedUpdate;

    private int _total = 0;
    private int _processed = 0;

    public GuiFileSortReporter(Action<int>? progressUpdate, Action<string>? elapsedUpdate, Action<int>? filesMovedUpdate)
    {
        _progressUpdate = progressUpdate;
        _elapsedUpdate = elapsedUpdate;
        _filesMovedUpdate = filesMovedUpdate;
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

    public void OnFileProcessed(string filePath, string category)
    {
        _processed++;
        var progress = (_total > 0) ? (_processed * 100) / _total : 0;
        _progressUpdate?.Invoke(progress);
    }

    public void OnFileSkipped(string filePath)
    {
        throw new NotImplementedException();
    }

    public void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts)
    {
        _filesMovedUpdate?.Invoke(totalMoved);
    }

    public void OnError(string filePath, Exception ex) { }

    public void OnDirectoryEmptied(string directoryPath) { }
}
