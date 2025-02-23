using System.Diagnostics;
using FileTidy.Services;

public class FileSorter
{
    private readonly string _directoryToSort;
    private readonly FileCategoryMapper _mapper;

    public FileSorter(string directoryToSort)
    {
        _directoryToSort = directoryToSort;
        _mapper = new FileCategoryMapper();
    }

    public void Sort()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        IEnumerable<string> allFiles = Directory.EnumerateFiles(_directoryToSort, "*", SearchOption.AllDirectories);
        int totalFiles = allFiles.Count();

        if (totalFiles == 0)
        {
            Console.WriteLine("No files found to sort.");
            return;
        }

        HashSet<string> createdDirectories = new();
        Dictionary<string, int> sortedSummary = new();
        int totalFilesMoved = 0, totalErrors = 0, processedFiles = 0;

        Console.WriteLine("\n🔄 Sorting in Progress (Preserving Folder Structure)...\n");

        foreach (var file in allFiles)
        {
            try
            {
                string extension = Path.GetExtension(file).ToLower();
                string category = _mapper.GetCategory(extension);
                
                // Get the relative path inside _directoryToSort
                string relativePath = Path.GetRelativePath(_directoryToSort, Path.GetDirectoryName(file)!);
                
                // Construct destination folder while preserving structure
                string destinationFolder = Path.Combine(_directoryToSort, category, relativePath);
                string destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(file));

                if (!createdDirectories.Contains(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                    createdDirectories.Add(destinationFolder);
                }

                string uniqueDestinationPath = GetUniqueFilePath(destinationFilePath);
                File.Move(file, uniqueDestinationPath);

                if (sortedSummary.ContainsKey(category))
                    sortedSummary[category]++;
                else
                    sortedSummary[category] = 1;

                totalFilesMoved++;
            }
            catch (Exception ex)
            {
                totalErrors++;
                Console.WriteLine($"❌ Error moving file: {file}");
                Console.WriteLine($"{ex.Message}");
            }

            processedFiles++;
            ConsoleProgress.DisplayProgressBar(processedFiles, totalFiles);
        }

        stopwatch.Stop();

        Console.WriteLine("\n✅ Sorting Complete (With Folder Structure Preserved)!\n");

        Console.WriteLine($"🔹 Total Files Processed: {totalFiles}");
        Console.WriteLine($"✅ Total Files Moved: {totalFilesMoved}");
        Console.WriteLine($"❌ Total Errors: {totalErrors}\n");
        Console.WriteLine($"⏳ Total Duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2} sec)\n");

        foreach (var entry in sortedSummary)
        {
            Console.WriteLine($"📂 {entry.Key}: {entry.Value} files moved.");
        }
    }

    /// <summary>
    /// Generates a unique filename if a duplicate exists.
    /// Example: report.pdf → report_1.pdf → report_2.pdf
    /// </summary>
    private string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        int count = 1;

        string newFilePath = filePath;

        while (File.Exists(newFilePath))
        {
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{count}{extension}");
            count++;
        }

        return newFilePath;
    }
}
