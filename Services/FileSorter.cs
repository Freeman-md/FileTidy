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
        IEnumerable<string> allFiles = Directory.EnumerateFiles(_directoryToSort, "*", SearchOption.AllDirectories);

        int totalFiles = allFiles.Count();
        int processedFiles = 0;

        if (totalFiles == 0)
        {
            Console.WriteLine("No files found to sort.");
            return;
        }

        Dictionary<string, string> fileDestinations = new Dictionary<string, string>();
        foreach (var file in allFiles)
        {
            string extension = Path.GetExtension(file).ToLower();
            string category = _mapper.GetCategory(extension);
            string destinationFolder = Path.Combine(_directoryToSort, category);
            fileDestinations[file] = destinationFolder;
        }

        HashSet<string> createdDirectories = new HashSet<string>();

        Dictionary<string, int> sortedSummary = new Dictionary<string, int>();
        int totalFilesMoved = 0, totalErrors = 0;

        Console.WriteLine("\n🔄 Sorting in Progress...\n");

        foreach (var fileDestination in fileDestinations)
        {
            string sourceFilePath = fileDestination.Key;
            string destinationFolder = fileDestination.Value;
            string destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(sourceFilePath));

            if (!createdDirectories.Contains(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
                createdDirectories.Add(destinationFolder);
            }

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

        Console.WriteLine("\n✅ Sorting Complete!\n");

        Console.WriteLine($"🔹 Total Files Processed: {totalFiles}");
        Console.WriteLine($"✅ Total Files Moved: {totalFilesMoved}");
        Console.WriteLine($"❌ Total Errors: {totalErrors}\n");

        foreach (var entry in sortedSummary)
        {
            Console.WriteLine($"📂 {entry.Key}: {entry.Value} files moved.");
        }
    }
}
