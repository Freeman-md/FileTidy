using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using FileTidy.Core.Utils;

namespace FileTidy.Core.Services;

public class FileOperationService : IFileOperationService
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
                string uniqueDestinationPath = PathUtils.GetUniqueFilePath(destinationFilePath);

                File.Move(file, uniqueDestinationPath);

                return new FileMoveResult
                {
                    OriginalPath = file,
                    NewPath = PathUtils.NormalizePath(uniqueDestinationPath),
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

    public async Task RemoveEmptyDirectoriesAsync(string directory)
    {
        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            await RemoveEmptyDirectoriesAsync(subDirectory);

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
} 