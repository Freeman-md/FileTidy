using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Contracts;

public interface IFolderService
{

    Task<List<FolderItem>> GetTopLevelFoldersAsync();
    Task<List<FolderItem>> GetFolderTreeAsync();

    Task<List<FileItem>> LoadFilesAsync(string folderPath);
}