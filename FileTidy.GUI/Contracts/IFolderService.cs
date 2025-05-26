using System.Collections.ObjectModel;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Contracts;

public interface IFolderService
{
    public ObservableCollection<FolderItem> GetSystemRootFolders();
}