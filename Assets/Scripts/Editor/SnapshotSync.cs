using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

class SnapshotAutoSync : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (importedAssets.Length > 0 || movedAssets.Length > 0 || deletedAssets.Length > 0)
        {
            SnapshotSync.SyncSnapshot();
        }
    }
}

public class SnapshotSync : EditorWindow
{
    private static string snapshotPath = @"D:/AutoDiggerSnapshot";

    // Folders to exclude from snapshot
    private static readonly List<string> excludedFolders = new List<string>
    {
        "Materials",
        "Textures",
        "Audio",
        "Packages",
        "Scenes" // only hierarchy summaries needed
    };

    [MenuItem("Tools/Sync Snapshot")]
    public static void ShowWindow()
    {
        GetWindow<SnapshotSync>("Snapshot Sync");
    }

    void OnGUI()
    {
        GUILayout.Label("Sync Project to Snapshot", EditorStyles.boldLabel);
        if (GUILayout.Button("Sync Snapshot Now"))
        {
            SyncSnapshot();
        }
    }

    public static void SyncSnapshot()
    {
        CopyScripts();
        CopyPrefabs();
        ExportHierarchySummaries();
        AssetDatabase.Refresh();
        Debug.Log("Snapshot synced!");
    }

    public static void CopyScripts()
    {
        string source = Application.dataPath + "/Scripts";
        string dest = snapshotPath + "/Assets/Scripts";
        MirrorDirectory(source, dest, ".cs");
    }

    public static void CopyPrefabs()
    {
        string source = Application.dataPath + "/Prefabs";
        string dest = snapshotPath + "/Assets/Prefabs";
        MirrorDirectory(source, dest, ".prefab");
    }

    public static void ExportHierarchySummaries()
    {
        string exportFolder = snapshotPath + "/HierarchySummaries";

        if (Directory.Exists(exportFolder))
            Directory.Delete(exportFolder, true);

        Directory.CreateDirectory(exportFolder);

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            string fileName = Path.Combine(exportFolder, scene.name + "_Hierarchy.txt");
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("Scene: " + scene.name);
                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    WriteObject(writer, rootObj, 0);
                }
            }
        }
    }

    private static void WriteObject(StreamWriter writer, GameObject obj, int indent)
    {
        string indentStr = new string(' ', indent * 2);
        string components = "";
        foreach (var comp in obj.GetComponents<MonoBehaviour>())
        {
            if (comp != null)
                components += comp.GetType().Name + ", ";
        }
        if (components.Length > 0)
            components = components.Substring(0, components.Length - 2);

        writer.WriteLine($"{indentStr}- {obj.name} ({components})");

        foreach (Transform child in obj.transform)
        {
            WriteObject(writer, child.gameObject, indent + 1);
        }
    }

    private static void MirrorDirectory(string sourceDir, string destinationDir, string extensionFilter)
    {
        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning("Source folder not found: " + sourceDir);
            return;
        }

        // Collect source files with filter
        var sourceFiles = Directory.GetFiles(sourceDir, "*" + extensionFilter, SearchOption.AllDirectories)
            .Where(f => !IsInExcludedFolder(f, sourceDir))
            .Select(f => f.Substring(sourceDir.Length + 1).Replace("\\", "/"))
            .ToList();

        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);

        foreach (var relativePath in sourceFiles)
        {
            string srcFile = Path.Combine(sourceDir, relativePath);
            string destFile = Path.Combine(destinationDir, relativePath);
            string destDir = Path.GetDirectoryName(destFile);
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            File.Copy(srcFile, destFile, true);
        }

        // Remove destination files that no longer exist in source
        var destFiles = Directory.GetFiles(destinationDir, "*" + extensionFilter, SearchOption.AllDirectories)
                                 .Select(f => f.Substring(destinationDir.Length + 1).Replace("\\", "/"))
                                 .ToList();

        foreach (var file in destFiles)
        {
            if (!sourceFiles.Contains(file))
                File.Delete(Path.Combine(destinationDir, file));
        }

        DeleteEmptyDirs(destinationDir);
    }

    private static bool IsInExcludedFolder(string filePath, string baseDir)
    {
        string relative = filePath.Substring(baseDir.Length + 1).Replace("\\", "/");
        return excludedFolders.Any(f => relative.StartsWith(f + "/"));
    }

    private static void DeleteEmptyDirs(string dir)
    {
        foreach (var subDir in Directory.GetDirectories(dir))
        {
            DeleteEmptyDirs(subDir);
            if (Directory.GetFiles(subDir).Length == 0 && Directory.GetDirectories(subDir).Length == 0)
                Directory.Delete(subDir);
        }
    }
}
