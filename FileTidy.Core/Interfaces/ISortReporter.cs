namespace FileTidy.Core.Interfaces;

public interface ISortReporter
{
    void OnFileProcessed(string filePath, string category);
    void OnError(string filePath, Exception ex);
    void OnDirectoryEmptied(string directoryPath);
    void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts);
}