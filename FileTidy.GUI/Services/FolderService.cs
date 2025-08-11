using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FileTidy.GUI.Contracts;
using FileTidy.GUI.Extensions;
using FileTidy.GUI.Models;

namespace FileTidy.GUI.Services;

public class FolderService : IFolderService
{
    public async Task<bool> CanAccessAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    return false;

                // Try to list just one entry to test access
                var _ = Directory.EnumerateFileSystemEntries(path).FirstOrDefault();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking access to {path}: {ex.Message}");
                return false;
            }
        });
    }
    
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
                        Size = 0,
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
                        Size = fileInfo.Length,
                        Modified = fileInfo.LastWriteTime.ToString("MMM dd, yyyy"),
                        FullPath = fileInfo.FullName
                    };
                });
            
            return folderItems.Concat(fileItems).ToList();
        });
    }

    public async Task OpenFolderAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        await Task.Run(() =>
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Linux and others
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open folder {path}: {ex.Message}");
            }
        });
    }

    public async Task OpenSystemFilesAndFoldersSettingsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Open macOS System Settings at Privacy > Files and Folders
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = "x-apple.systempreferences:com.apple.preference.security?Privacy_FilesAndFolders",
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // No exact equivalent; open Settings home or a helpful URL
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:privacy",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Linux: generic settings not standardized; no-op
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open system Files & Folders settings: {ex.Message}");
            }
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