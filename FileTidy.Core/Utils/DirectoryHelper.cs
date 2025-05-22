namespace FileTidy.Core.Utils;

/// <summary>
/// Provides common directory-related utility methods for resolving paths, checking existence,
/// and retrieving files recursively.
/// </summary>
public static class DirectoryHelper
{
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

        if (path.Equals("testing", StringComparison.OrdinalIgnoreCase))
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
