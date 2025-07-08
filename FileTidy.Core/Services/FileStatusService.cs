using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileTidy.Core.Services;

public class FileStatusService : IFileStatusService
{
    private readonly IFileOperationStore _fileOperationStore;

    public FileStatusService(IFileOperationStore fileOperationStore)
    {
        _fileOperationStore = fileOperationStore;
    }


    public async Task<Dictionary<string, FileOperationStatus>> GetFileStatusesForDirectoryAsync(string folderPath)
    {
        var fileOperations = await _fileOperationStore.GetFileOperationsInDirectoryAsync(folderPath);
        
        var filteredOperations = fileOperations
            .GroupBy(operation => operation.NewPath)
            .Select(group => group.OrderByDescending(operation => operation.Timestamp).First())
            .ToDictionary(operation => operation.NewPath, operation => operation.Status);
        
        return filteredOperations;
    }
} 