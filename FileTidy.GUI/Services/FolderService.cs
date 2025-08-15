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
        var candidatePaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };

        // Check existence + access
        var checks = candidatePaths
            .Where(Directory.Exists)
            .Select(async path => new { path, can = await CanAccessAsync(path) });

        var results = await Task.WhenAll(checks);
        return results
            .Where(result => result.can)
            .Select(result => new FolderItem
            {
                Name = Path.GetFileName(result.path),
                FullPath = result.path,
                SubFolders = new List<FolderItem>()
            })
            .ToList();
    }

    public async Task<List<FolderItem>> GetFolderTreeAsync()
    {
        var roots = await GetTopLevelFoldersAsync();

        return roots.Select(root => new FolderItem
        {
            Name = root.Name,
            FullPath = root.FullPath,
            SubFolders = GetSubFolders(root.FullPath)
        }).ToList();
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
                using var process = new Process();
                process.StartInfo.UseShellExecute = true;

                if (OperatingSystem.IsMacOS())
                    process.StartInfo.FileName = "x-apple.systempreferences:com.apple.preference.security?Privacy_FilesAndFolders";
                else if (OperatingSystem.IsWindows())
                    process.StartInfo.FileName = "ms-settings:privacy-broadfilesystemaccess";
                else if (OperatingSystem.IsLinux())
                {
                    process.StartInfo.FileName = "xdg-open";
                    process.StartInfo.Arguments = "https://wiki.gnome.org/Design/OS/Privacy";
                    process.StartInfo.UseShellExecute = false;
                }

                process.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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