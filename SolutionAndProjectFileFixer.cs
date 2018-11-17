using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace UnityExtensions
{

    [UnityEditor.InitializeOnLoad]
    internal static class SolutionFileFixer
    {

        private static readonly string ProjectPath =
            Path.GetDirectoryName(UnityEngine.Application.dataPath);

        static SolutionFileFixer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            SolutionFileWatcher.solutionFileChanged += OnSolutionFileChanged;
            SolutionFileWatcher.csprojFileChanged += OnCsprojFileChanged;
            SolutionFileWatcher.Start();
            FixSolutionFile();
        }

        //----------------------------------------------------------------------

        private static void OnBeforeAssemblyReload()
        {
            SolutionFileWatcher.Stop();
        }

        private static void OnSolutionFileChanged(string filePath)
        {
            UnityEditor.EditorApplication.delayCall += () =>
                FixSolutionFile(filePath);
        }

        private static void OnCsprojFileChanged(string filePath)
        {
            UnityEditor.EditorApplication.delayCall += () =>
                FixCsprojFile(filePath);
        }

        //----------------------------------------------------------------------

        private const string
        OldOutputPath = @"<OutputPath>Temp\bin\Debug\</OutputPath>",
        NewOutputPath = @"<OutputPath>Library\ScriptAssemblies\</OutputPath>";

        private static void FixCsprojFile(string csprojPath)
        {
            var text = File.ReadAllText(csprojPath);
            if (text.Contains(OldOutputPath))
            {
                text = text.Replace(OldOutputPath, NewOutputPath);
                File.WriteAllText(csprojPath, text);
                UnityEngine.Debug.Log($"fixed csproj file: {csprojPath}");
            }
        }

        private static void FixCsprojFiles()
        {
            var filePaths = Directory.EnumerateFiles(ProjectPath);
            foreach (var filePath in filePaths)
                if (filePath.EndsWith(".csproj"))
                    FixCsprojFile(filePath);
        }

        //----------------------------------------------------------------------

        private static string FindSolutionFile()
        {
            var projectName = Path.GetFileNameWithoutExtension(ProjectPath);
            var solutionFile = $"{projectName}.sln";
            var solutionPath = Path.Combine(ProjectPath, solutionFile);
            if (File.Exists(solutionPath))
                return solutionPath;
            return null;
        }

        [UnityEditor.MenuItem("Solution/Fix")]
        private static void FixSolutionFile()
        {
            var solutionPath = FindSolutionFile();
            if (solutionPath == null)
            {
                UnityEngine.Debug.LogError("solution file not found");
                return;
            }
            FixSolutionFile(solutionPath);
            FixCsprojFiles();
        }

        private static void FixSolutionFile(string solutionPath)
        {
            var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
            var fixPattern = $"( = \"{solutionName}\",) \"(.*)[.]csproj\"";
            var fixRegex = new Regex(fixPattern);
            var fixCount = 0;
            var lines = File.ReadAllLines(solutionPath);
            for (int i = 0, n = lines.Length; i < n; ++i)
            {
                var line = lines[i];
                if (line.StartsWith("Project(\"{") && fixRegex.IsMatch(line))
                {
                    var matches = fixRegex.Matches(line);
                    foreach (Match match in matches)
                    {
                        var csprojName = match.Groups[2].Value;
                        if (csprojName == solutionName)
                            continue;

                        var oldSubstring = match.Groups[1].Value;
                        var newSubstring = $" = \"{csprojName}\",";
                        if (newSubstring == oldSubstring)
                            continue;

                        fixCount += 1;
                        lines[i] = line.Replace(oldSubstring, newSubstring);
                    }
                }
            }
            if (fixCount > 0)
            {
                var writeTime = File.GetLastWriteTime(solutionPath);
                SolutionFileWatcher.Pause();
                var text = string.Join("\n", lines);
                File.WriteAllText(solutionPath, text);
                File.SetLastWriteTime(solutionPath, writeTime);
                SolutionFileWatcher.Resume();
                UnityEngine.Debug.Log($"fixed solution file: {solutionPath}");
            }
        }

    }

}