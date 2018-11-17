using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityExtensions
{

    internal static class SolutionFileWatcher
    {

        private static readonly object Mutex = new object();

        private static FileSystemWatcher s_fileSystemWatcher;

        private static Action<string> s_solutionFileChanged;

        private static Action<string> s_csprojFileChanged;

        private static string s_projectPath;

        static SolutionFileWatcher()
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER","enabled");
            #endif
            var assetsPath = Application.dataPath;
            s_projectPath = Path.GetDirectoryName(assetsPath);
        }

        internal static event Action<string> solutionFileChanged
        {
            add { lock (Mutex) s_solutionFileChanged += value; }
            remove { lock (Mutex) s_solutionFileChanged -= value; }
        }

        internal static event Action<string> csprojFileChanged
        {
            add { lock (Mutex) s_csprojFileChanged += value; }
            remove { lock (Mutex) s_csprojFileChanged -= value; }
        }

        internal static void Start()
        {
            Stop();
            lock (Mutex)
            {
                s_fileSystemWatcher = new FileSystemWatcher() {
                    Path = s_projectPath,
                    NotifyFilter =
                        NotifyFilters.FileName |
                        NotifyFilters.Size |
                        NotifyFilters.LastWrite |
                        NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true,
                };
                s_fileSystemWatcher.Changed += OnChanged;
            }
        }

        internal static void Stop()
        {
            lock (Mutex)
            {
                if (s_fileSystemWatcher != null)
                {
                    s_fileSystemWatcher.Dispose();
                    s_fileSystemWatcher = null;
                }
            }
        }

        internal static void Pause()
        {
            lock (Mutex)
            {
                if (s_fileSystemWatcher != null)
                {
                    s_fileSystemWatcher.EnableRaisingEvents = false;
                }
            }
        }

        internal static void Resume()
        {
            lock (Mutex)
            {
                if (s_fileSystemWatcher != null)
                {
                    s_fileSystemWatcher.EnableRaisingEvents = true;
                    return;
                }
            }
            Start();
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Deleted:
                    return;
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                    var filePath = e.FullPath;
                    if (filePath.EndsWith(".sln"))
                    {
                        var callback = default(Action<string>);
                        lock (Mutex) callback = s_solutionFileChanged;
                        if (callback != null)
                            callback(filePath);
                    }
                    if (filePath.EndsWith(".csproj"))
                    {
                        var callback = default(Action<string>);
                        lock (Mutex) callback = s_csprojFileChanged;
                        if (callback != null)
                            callback(filePath);
                    }
                    return;
            }
        }

    }

}