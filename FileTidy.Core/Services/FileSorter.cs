using System.Diagnostics;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Services;

namespace FileTidy.Core.Services;

public class FileSorter
{
    private readonly string _directoryToSort;
    private readonly FileCategoryMapper _mapper;
    private readonly ISortReporter? _reporter;

    public FileSorter(string directoryToSort, FileCategoryMapper mapper, ISortReporter? reporter = null)
    {
        _directoryToSort = directoryToSort;
        _mapper = mapper;
        _reporter = reporter;
    }

    public void Sort()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        IEnumerable<string> allFiles = Directory.EnumerateFiles(_directoryToSort, "*", SearchOption.AllDirectories);
        int totalFiles = allFiles.Count();

        if (totalFiles == 0)
            return;

        HashSet<string> createdDirectories = new();
        Dictionary<string, int> sortedSummary = new();
        int totalFilesMoved = 0, totalErrors = 0, processedFiles = 0;

        foreach (var file in allFiles)
        {

            try
            {
                string extension = Path.GetExtension(file).ToLower();
                string category = _mapper.GetCategory(extension);

                if (Path.GetFullPath(file).Contains(Path.Combine(_directoryToSort, category) + Path.DirectorySeparatorChar))
                    continue;

                // Get the subdirectory path (relative to _directoryToSort) where the current file is located
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

                _reporter?.OnFileProcessed(file, category);
            }
            catch (Exception ex)
            {
                totalErrors++;
                _reporter?.OnError(file, ex);
            }

            processedFiles++;
        }

        RemoveEmptyDirectories(_directoryToSort);

        stopwatch.Stop();

        _reporter?.OnSummary(
            totalFiles,
            totalFilesMoved,
            totalErrors,
            sortedSummary
        );
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

    /// <summary>
    /// Recursively removes empty directories inside the sorted directory.
    /// </summary>
    private void RemoveEmptyDirectories(string directory)
    {
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            RemoveEmptyDirectories(subDirectory); // Recursively check and remove empty subdirectories

            // Delete only if the directory is truly empty
            if (!Directory.EnumerateFileSystemEntries(subDirectory).Any())
            {
                try
                {
                    Directory.Delete(subDirectory);
                    _reporter?.OnDirectoryEmptied(subDirectory);
                }
                catch (Exception ex)
                {
                    _reporter?.OnError(subDirectory, ex);
                }
            }
        }
    }

}
