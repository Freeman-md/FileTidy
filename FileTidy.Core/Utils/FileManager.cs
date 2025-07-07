using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;

namespace FileTidy.Core.Utils;

public class FileManager : IFileManager
{
    public Task<FileMoveResult> MoveFileAsync(string file, string category, string directoryPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fullCategoryPath = Path.Combine(directoryPath, category);
                if (Path.GetFullPath(file).Contains(fullCategoryPath + Path.DirectorySeparatorChar))
                {
                    return new FileMoveResult
                    {
                        OriginalPath = file,
                        NewPath = file,
                        Status = FileOperationStatus.Skipped
                    };
                }

                string relativePath = Path.GetRelativePath(directoryPath, Path.GetDirectoryName(file)!);
                string destinationFolder = Path.Combine(directoryPath, category, relativePath);
                Directory.CreateDirectory(destinationFolder);

                string destinationFilePath = Path.Combine(destinationFolder, Path.GetFileName(file));
                string uniqueDestinationPath = GetUniqueFilePath(destinationFilePath);

                File.Move(file, uniqueDestinationPath);

                return new FileMoveResult
                {
                    OriginalPath = file,
                    NewPath = NormalizePath(uniqueDestinationPath),
                    Status = FileOperationStatus.Moved
                };
            }
            catch (Exception ex)
            {
                return new FileMoveResult
                {
                    OriginalPath = file,
                    NewPath = file,
                    Status = FileOperationStatus.Failed,
                    Error = ex
                };
            }
        }, cancellationToken);
    }

    
    public Task RevertFileAsync(string fromPath, string toPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationDirectory = Path.GetDirectoryName(toPath);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory!);
            }

            if (File.Exists(toPath))
            {
                File.Delete(toPath);
            }

            File.Move(fromPath, toPath);
        }, cancellationToken);
    }
    
    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            File.Delete(path);
        }, cancellationToken);
    }

    public async Task RemoveEmptyDirectories(string directory)
    {
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            await RemoveEmptyDirectories(subDirectory);

            if (!Directory.EnumerateFileSystemEntries(subDirectory).Any())
            {
                try
                {
                    Directory.Delete(subDirectory);
                }
                catch (Exception)
                {
                    // Optionally log or handle error
                }
            }
        }
    }

    public async Task RetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 200)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                await action();
                break;
            }
            catch
            {
                attempt++;
                if (attempt >= maxRetries)
                    throw;
                await Task.Delay(delayMs);
            }
        }
    }

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
    
    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .Replace("\\", "/")
            .Replace("/./", "/")
            .TrimEnd('/');
    }

}
