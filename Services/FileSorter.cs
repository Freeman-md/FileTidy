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

        // Store file destinations
        Dictionary<string, string> fileDestinations = new();
        foreach (var file in allFiles)
        {
            string extension = Path.GetExtension(file).ToLower();
            string category = _mapper.GetCategory(extension);
            string destinationFolder = Path.Combine(_directoryToSort, category);
            fileDestinations[file] = destinationFolder;
        }

        // Ensure directories exist first
        HashSet<string> createdDirectories = new();
        foreach (var destination in fileDestinations.Values.Distinct())
        {
            if (!createdDirectories.Contains(destination))
            {
                Directory.CreateDirectory(destination);
                createdDirectories.Add(destination);
            }
        }

        int totalFilesMoved = 0, processedFiles = 0, totalErrors = 0;
        Dictionary<string, int> sortedSummary = new();

        Console.WriteLine("\n🔄 Sorting in Progress...\n");

        foreach (var fileDestination in fileDestinations)
        {
            string sourceFilePath = fileDestination.Key;
            string destinationFolder = fileDestination.Value;
            string destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(sourceFilePath));

            try
            {
                if (File.Exists(destinationFilePath))
                {
                    string newFilePath = Path.Combine(destinationFolder,
                        $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_copy{Path.GetExtension(sourceFilePath)}");
                    File.Move(sourceFilePath, newFilePath);
                }
                else
                {
                    File.Move(sourceFilePath, destinationFilePath);
                }

                string category = new DirectoryInfo(destinationFolder).Name;
                if (sortedSummary.ContainsKey(category))
                    sortedSummary[category]++;
                else
                    sortedSummary[category] = 1;

                totalFilesMoved++;
            }
            catch (Exception ex)
            {
                totalErrors++;
                Console.WriteLine($"❌ Error moving file: {sourceFilePath} → {destinationFolder}");
                Console.WriteLine($"{ex.Message}");
            }

            processedFiles++;
            ConsoleProgress.DisplayProgressBar(processedFiles, totalFiles);
        }

        stopwatch.Stop();

        Console.WriteLine("\n✅ Sorting Complete!\n");

        Console.WriteLine($"🔹 Total Files Processed: {totalFiles}");
        Console.WriteLine($"✅ Total Files Moved: {totalFilesMoved}");
        Console.WriteLine($"❌ Total Errors: {totalErrors}\n");
        Console.WriteLine($"⏳ Total Duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2} sec)\n");

        foreach (var entry in sortedSummary)
        {
            Console.WriteLine($"📂 {entry.Key}: {entry.Value} files moved.");
        }
    }
}
