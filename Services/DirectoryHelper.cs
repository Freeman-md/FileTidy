namespace FileTidy.Services;

public static class DirectoryHelper
{
    public static string GetFullPath(string path)
    {
        if (path.Equals("downloads", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
        else if (path.Equals("testing", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", path);
        }
        else if (path.Equals("documents", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
        else if (path.Equals("desktop", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }

        return Path.GetFullPath(path);
    }

    public static bool CheckIfDirectoryExists(string path)
    {
        if (Directory.Exists(path))
        {
            Console.WriteLine($"\n✅ Directory found: {path}");
            IEnumerable<string> allFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            Console.WriteLine($"📂 Total files in '{path}': {allFiles.Count()}");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ Directory does not exist: {path}");
            return false;
        }
    }
}
