using FileTidy.Core.Interfaces;
using FileTidy.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileTidy.Core.Services;

public class FileStatusService : IFileStatusService
{
    public Task<Dictionary<string, FileOperationStatus>> GetFileStatusesForDirectoryAsync(string folderPath)
        => throw new System.NotImplementedException();
} 