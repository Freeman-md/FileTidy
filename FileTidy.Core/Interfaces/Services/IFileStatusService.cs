namespace FileTidy.Core.Interfaces;

using FileTidy.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFileStatusService
{
    Task<Dictionary<string, FileOperationStatus>> GetFileStatusesForDirectoryAsync(string folderPath);
} 