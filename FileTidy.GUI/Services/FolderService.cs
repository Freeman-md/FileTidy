using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Services;

public class FolderService : IFolderService
{
    public ObservableCollection<FolderItem> GetSystemRootFolders()
    {
        var rootFolderPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };
        
        var rootFolderItems = new ObservableCollection<FolderItem>();

        foreach (var rootFolderPath in rootFolderPaths)
        {
            if (!Directory.Exists(rootFolderPath)) continue;

            var rootFolderItem = new FolderItem
            {
                Name = Path.GetFileName(rootFolderPath),
                FullPath = rootFolderPath,
                SubFolders = GetSubFolders(rootFolderPath)
            };
            
            rootFolderItems.Add(rootFolderItem);
        }
        
        return rootFolderItems;
    }

    private static List<FolderItem> GetSubFolders(string rootFolderPath)
    {
        var list = new List<FolderItem>();

        try
        {
            foreach (var directory in Directory.GetDirectories(rootFolderPath))
            {
                list.Add(new FolderItem
                {
                    Name = Path.GetFileName(directory),
                    FullPath = directory
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return list;
    }
}