using FileTidy.Core.Models;

namespace FileTidy.Core.Interfaces;

public interface IFileOperationLookupService
{
    Task<Dictionary<string, FileOperationStatus>> GetFileStatusesForDirectoryAsync(string folderPath);
}