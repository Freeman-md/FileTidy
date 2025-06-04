using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;

namespace FileTidy.Core.Services;

public class FileOperationLookupService : IFileOperationLookupService
{
    private readonly IFileOperationStore _fileOperationStore;

    public FileOperationLookupService(IFileOperationStore fileOperationStore)
    {
        _fileOperationStore = fileOperationStore;
    }


    public async Task<Dictionary<string, FileOperationStatus>> GetFileStatusesForDirectoryAsync(string folderPath)
    {
        var fileOperations = await _fileOperationStore.GetFileOperationsInDirectoryAsync(folderPath);
        
        var filteredOperations = fileOperations
            .Where(operation => Path.GetDirectoryName(operation.NewPath)?.TrimEnd(Path.DirectorySeparatorChar) == folderPath.TrimEnd(Path.DirectorySeparatorChar))
            .GroupBy(operation => operation.NewPath)
            .Select(group => group.OrderByDescending(operation => operation.Timestamp).First())
            .ToDictionary(operation => operation.NewPath, operation => operation.Status);
        
        return filteredOperations;
    }
}