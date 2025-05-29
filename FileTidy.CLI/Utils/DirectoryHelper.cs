namespace FileTidy.CLI.Utils;

public static class DirectoryHelper
{
    public static bool CheckIfDirectoryExists(string path)
    {
        if (DirectoryHelper.DirectoryExists(path))
        {
            Console.WriteLine($"\n✅ Directory found: {path}");
            var allFiles = DirectoryHelper.GetAllFiles(path);
            Console.WriteLine($"📂 Total files in '{path}': {allFiles.Count()}");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ Directory does not exist: {path}");
            return false;
        }
    }

    /// <summary>
    /// Resolves a user-friendly folder keyword (e.g., "downloads", "desktop") to its full absolute path.
    /// Falls back to Path.GetFullPath if the keyword is unrecognized.
    /// </summary>
    /// <param name="path">The user input path or keyword.</param>
    /// <returns>The absolute path corresponding to the keyword or input.</returns>
    public static string GetFullPath(string path)
    {
        if (path.Equals("downloads", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        if (path.Equals("test-folder", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", path);

        if (path.Equals("documents", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        if (path.Equals("desktop", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Checks whether the specified directory exists.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>True if the directory exists; otherwise, false.</returns>
    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <summary>
    /// Recursively enumerates all files within the specified directory and its subdirectories.
    /// </summary>
    /// <param name="path">The root directory to search.</param>
    /// <returns>An enumerable of full file paths.</returns>
    public static IEnumerable<string> GetAllFiles(string path)
    {
        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
    }
}
