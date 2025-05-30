using System.Diagnostics;
using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;

namespace FileTidy.Core.Services;

public class FileTidyingService : IFileTidyingService
{
    private readonly FileCategoryMapper _mapper;
    private readonly ISortReporter? _reporter;

    public FileTidyingService(ISortReporter? reporter = null, string? dataPath = null)
    {
        _reporter = reporter;

        string resolvedPath = dataPath ?? Path.Combine(Path.GetDirectoryName(typeof(FileTidyingService).Assembly.Location)!, "Data");
        _mapper = new FileCategoryMapper(resolvedPath);
    }

    public async Task<TidyingResult> SortDirectory(string directoryPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var filesToProcess = GetFilesToProcess(directoryPath);

            if (!filesToProcess.Any())
            {
                return new TidyingResult
                {
                    TotalFiles = 0,
                    TotalMoved = 0,
                    TotalErrors = 0,
                    PerCategoryCounts = new(),
                    Elapsed = TimeSpan.Zero
                };
            }

            _reporter?.SetTotalFiles(filesToProcess.Count);

            var sorter = new FileSorter(directoryPath, _mapper, _reporter);
            var result = sorter.Sort(filesToProcess, cancellationToken);

            return result;

        }, cancellationToken);
    }

    private List<string> GetFilesToProcess(string directoryPath)
    {
        var allFiles = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();

        var categoryFolders = _mapper.GetAllCategoryNames()
            .Select(c => Path.Combine(directoryPath, c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allFiles
            .Where(file =>
                !categoryFolders.Any(folder =>
                    file.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

}