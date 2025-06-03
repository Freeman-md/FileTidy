namespace FileTidy.Core.Interfaces;

public interface ISortReporter
{
    void OnFileProcessed(string filePath, string category);
    void OnFileSkipped(string filePath);
    void OnError(string? filePath, Exception ex);
    void OnDirectoryEmptied(string directoryPath);
    void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts);
    void SetTotalFiles(int total);
    void OnElapsedTimeReported(TimeSpan elapsed);
    void OnFileReverted(string operationNewPath);
    void OnSessionReverted(Guid sessionId);
}
