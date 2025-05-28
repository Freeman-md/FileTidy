using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Extensions;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Services;

public class FolderService : IFolderService
{
    public async Task<List<FolderItem>> GetSystemRootFolders()
    {
        return await Task.Run(() =>
        {
            var rootFolderPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            var rootFolderItems = new List<FolderItem>();

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
        });
    }

    public async Task<List<FileItem>> LoadFilesAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return new List<FileItem>();
        
        var filePaths = Directory.GetFiles(folderPath);

        var result = await Task.Run(() => filePaths.Select(path => new FileItem
        {
            Name = Path.GetFileName(path),
            Type = Path.GetExtension(path).TrimStart('.').ToUpper(),
            Size = new FileInfo(path).Length.BytesToReadableSize(),
            Modified = File.GetLastWriteTime(path).ToString("MMM dd, yyyy"),
            Status = "Unprocessed"
        }).ToList());

        return result;
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
                    FullPath = directory,
                    SubFolders = GetSubFolders(directory)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Access denied or error accessing: {rootFolderPath} — {ex.Message}");
        }

        return list;
    }
}