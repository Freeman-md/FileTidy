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
    public async Task<List<FolderItem>> GetTopLevelFoldersAsync()
    {
        return await Task.Run(() =>
        {
            var folderPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            return folderPaths
                .Where(Directory.Exists)
                .Select(path => new FolderItem
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    SubFolders = new List<FolderItem>() // Empty for now
                })
                .ToList();
        });
    }

    public async Task<List<FolderItem>> GetFolderTreeAsync()
    {
        return await Task.Run(() =>
        {
            var folderPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            return folderPaths
                .Where(Directory.Exists)
                .Select(path => new FolderItem
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    SubFolders = GetSubFolders(path)
                })
                .ToList();
        });
    }


    public async Task<List<FileItem>> LoadFilesAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return new List<FileItem>();

        return await Task.Run(() =>
        {
            var folderItems = Directory.GetDirectories(folderPath)
                .Select(path =>
                {
                    var directoryInfo = new DirectoryInfo(path);
                    return new FileItem
                    {
                        Name = Path.GetFileName(path),
                        Type = "FOLDER",
                        Size = "-",
                        Modified = directoryInfo.LastWriteTime.ToString("MMM dd, yyyy"),
                        FullPath = directoryInfo.FullName
                    };
                });

            var fileItems = Directory.GetFiles(folderPath)
                .Select(path =>
                {
                    var fileInfo = new FileInfo(path);

                    return new FileItem
                    {
                        Name = Path.GetFileName(path),
                        Type = Path.GetExtension(path).TrimStart('.').ToUpper(),
                        Size = fileInfo.Length.BytesToReadableSize(),
                        Modified = fileInfo.LastWriteTime.ToString("MMM dd, yyyy"),
                    };
                });
            
            return folderItems.Concat(fileItems).ToList();
        });
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