namespace FileTidy.Core.Utils;

public static class DirectoryHelper
{
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

    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public static IEnumerable<string> GetAllFiles(string path)
    {
        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
    }
}
