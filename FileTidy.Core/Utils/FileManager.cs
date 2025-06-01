using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;

namespace FileTidy.Core.Utils;

public class FileManager : IFileManager
{
    public FileMoveResult MoveFile(string file, string category, string directoryPath, CancellationToken cancellationToken = default)
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
                NewPath = uniqueDestinationPath,
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
}
