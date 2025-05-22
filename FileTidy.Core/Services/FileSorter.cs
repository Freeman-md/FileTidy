using System.Diagnostics;
using FileTidy.Core.Interfaces;

namespace FileTidy.Core.Services;

/// <summary>
/// Handles the sorting of files in a given directory by file extension categories,
/// preserving the original folder structure and reporting progress through an optional reporter.
/// </summary>
public class FileSorter
{
    private readonly string _directoryToSort;
    private readonly FileCategoryMapper _mapper;
    private readonly ISortReporter? _reporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSorter"/> class.
    /// </summary>
    /// <param name="directoryToSort">The root directory to scan and sort files in.</param>
    /// <param name="mapper">The category mapper for determining file categories.</param>
    /// <param name="reporter">An optional progress reporter for UI or logging.</param>
    public FileSorter(string directoryToSort, FileCategoryMapper mapper, ISortReporter? reporter = null)
    {
        _directoryToSort = directoryToSort;
        _mapper = mapper;
        _reporter = reporter;
    }

    /// <summary>
    /// Sorts all files in the directory (and subdirectories) into category folders,
    /// preserving their relative paths and skipping already sorted files.
    /// </summary>
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

                // Skip if file is already inside its category folder
                if (Path.GetFullPath(file).Contains(Path.Combine(_directoryToSort, category) + Path.DirectorySeparatorChar))
                    continue;

                string relativePath = Path.GetRelativePath(_directoryToSort, Path.GetDirectoryName(file)!);
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
    /// Ensures a unique destination path if a file with the same name already exists.
    /// Appends an incrementing counter to the filename.
    /// </summary>
    /// <param name="filePath">The desired destination path.</param>
    /// <returns>A unique file path that avoids collisions.</returns>
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
    /// Recursively removes all empty directories from the given directory,
    /// including nested empty subfolders.
    /// </summary>
    /// <param name="directory">The root directory to clean up.</param>
    private void RemoveEmptyDirectories(string directory)
    {
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            RemoveEmptyDirectories(subDirectory);

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
