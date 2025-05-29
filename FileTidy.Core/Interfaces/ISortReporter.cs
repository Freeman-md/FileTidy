namespace FileTidy.Core.Interfaces;

/// <summary>
/// Interface for reporting the progress, status, and results of the file sorting process.
/// Implementations may provide console output, UI updates, logs, or other reporting methods.
/// </summary>
public interface ISortReporter
{
    /// <summary>
    /// Called each time a file is successfully processed and sorted.
    /// </summary>
    /// <param name="filePath">The full path of the file that was processed.</param>
    /// <param name="category">The category the file was moved to.</param>
    void OnFileProcessed(string filePath, string category);

    /// <summary>
    /// Called when an error occurs during processing.
    /// </summary>
    /// <param name="filePath">The path of the file or directory that caused the error.</param>
    /// <param name="ex">The exception that was thrown.</param>
    void OnError(string filePath, Exception ex);

    /// <summary>
    /// Called when an empty directory has been successfully removed.
    /// </summary>
    /// <param name="directoryPath">The full path of the directory that was deleted.</param>
    void OnDirectoryEmptied(string directoryPath);

    /// <summary>
    /// Called after all files have been processed to report the overall result.
    /// </summary>
    /// <param name="totalFiles">Total number of files scanned.</param>
    /// <param name="totalMoved">Total number of files successfully moved.</param>
    /// <param name="totalErrors">Total number of errors encountered.</param>
    /// <param name="perCategoryCounts">Breakdown of how many files were moved into each category.</param>
    void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts);

    /// <summary>
    /// Sets the total number of files to process. Useful for initializing progress indicators.
    /// </summary>
    /// <param name="total">The total count of files to be sorted.</param>
    void SetTotalFiles(int total);

    void ReportElapsed(TimeSpan elapsed);
}
