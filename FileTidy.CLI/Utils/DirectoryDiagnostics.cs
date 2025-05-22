using FileTidy.Core.Utils;

namespace FileTidy.CLI.Utils;

public static class DirectoryDiagnostics
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
}
