using FileTidy.Core.Interfaces;

namespace FileTidy.CLI.Reporting;

public class ConsoleSortReporter : ISortReporter
{
    private int _processed = 0;
    private int _total = 0;

    public void OnFileProcessed(string filePath, string category)
    {
        _processed++;
        DisplayProgressBar(_processed, _total);
    }

    public void OnFileSkipped(string filePath)
    {
        Console.WriteLine($"\n⚠️ Skipped file (already sorted or unsupported): {filePath}");
    }

    public void OnError(string filePath, Exception ex)
    {
        Console.WriteLine($"\n❌ Error moving file: {filePath}");
        Console.WriteLine($"{ex.Message}");
    }

    public void OnDirectoryEmptied(string directoryPath)
    {
        Console.WriteLine($"🗑️ Removed empty folder: {directoryPath}");
    }

    public void OnSummary(int totalFiles, int totalMoved, int totalErrors, Dictionary<string, int> perCategoryCounts)
    {
        Console.WriteLine("\n✅ Sorting Complete!\n");
        Console.WriteLine($"🔹 Total Files Processed: {totalFiles}");
        Console.WriteLine($"✅ Total Files Moved: {totalMoved}");
        Console.WriteLine($"❌ Total Errors: {totalErrors}\n");

        foreach (var entry in perCategoryCounts)
        {
            Console.WriteLine($"📂 {entry.Key}: {entry.Value} files moved.");
        }
    }

    public void SetTotalFiles(int total)
    {
        _total = total;
    }

    public void OnElapsedTimeReported(TimeSpan elapsed)
    {
        Console.WriteLine($"\n⏱️ Elapsed time so far: {elapsed:mm\\:ss}");
    }

    private void DisplayProgressBar(int current, int total)
    {
        int barWidth = 50;
        double progress = (double)current / total;
        int filledLength = (int)(progress * barWidth);

        Console.CursorLeft = 0;
        Console.Write("[");
        Console.Write(new string('█', filledLength));
        Console.Write(new string(' ', barWidth - filledLength));
        Console.Write($"] {current}/{total} ({progress * 100:F1}%)");
    }

}