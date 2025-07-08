namespace FileTidy.Core.Utils;

public static class PathUtils
{
    public static string GetUniqueFilePath(string filePath)
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
    
    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .Replace("\\", "/")
            .Replace("/./", "/")
            .TrimEnd('/');
    }
}