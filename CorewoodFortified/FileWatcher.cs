using System;
using System.Collections.Generic;
using System.IO;
using Jotunn;

namespace CorewoodFortified;

internal class FileWatcher
{
    private static readonly List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

    public static void WatchFileChanges(string path, Action onChanged, bool includeSubdirectories = false)
    {
        FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        string directoryName = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        fileSystemWatcher.Path = directoryName;
        fileSystemWatcher.Filter = fileName;
        fileSystemWatcher.IncludeSubdirectories = includeSubdirectories;
        fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        fileSystemWatcher.Changed += delegate
        {
            RaiseChanged(onChanged);
        };
        fileSystemWatcher.Deleted += delegate
        {
            RaiseChanged(onChanged);
        };
        fileSystemWatcher.Created += delegate
        {
            RaiseChanged(onChanged);
        };
        fileSystemWatcher.Renamed += delegate
        {
            RaiseChanged(onChanged);
        };
        fileSystemWatcher.EnableRaisingEvents = true;
        Watchers.Add(fileSystemWatcher);
    }

    public static void WatchTranslationChanges(string path, Action onChanged)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        try
        {
            WatchFileChanges(Path.Combine(path, "*.*"), onChanged, includeSubdirectories: true);
        }
        catch (Exception ex)
        {
            Logger.LogError("There was an issue watching Translation files: " + ex.Message);
        }
    }

    private static void RaiseChanged(Action onChanged)
    {
        try
        {
            onChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogWarning("[CorewoodFortified] Failed to reload changed file: " + ex.Message);
        }
    }
}
