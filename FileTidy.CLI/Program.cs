using FileTidy.CLI.Handlers;
using FileTidy.CLI.Utils;
using FileTidy.Core.Utils;

List<string> directoriesToSort = new();

while (true)
{
    Console.Write("Enter folder path(s) (comma-separated) or type 'exit' to quit: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Trim().ToLower() == "exit") break;

    var paths = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Trim());

    foreach (var path in paths)
    {
        string fullPath = DirectoryHelper.GetFullPath(path);

        if (DirectoryDiagnostics.CheckIfDirectoryExists(fullPath))
        {
            directoriesToSort.Add(fullPath);
        }
    }

    if (directoriesToSort.Any())
    {
        SortingHandler.SortDirectories(directoriesToSort);
        directoriesToSort.Clear();
    }
}
