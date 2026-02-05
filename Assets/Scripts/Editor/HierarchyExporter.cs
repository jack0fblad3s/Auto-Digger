using UnityEngine;
using UnityEditor;
using System.IO;

public class HierarchyExporter : EditorWindow
{
    [MenuItem("Tools/Export Hierarchy Summary")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyExporter>("Hierarchy Exporter");
    }

    private string exportFolder = "D:/AutoDiggerSnapshot/HierarchySummaries";

    void OnGUI()
    {
        GUILayout.Label("Export Hierarchy Summary", EditorStyles.boldLabel);

        if (GUILayout.Button("Export All Open Scenes"))
        {
            ExportAllOpenScenes();
        }
    }

    static void ExportAllOpenScenes()
    {
        string exportPath = "D:/AutoDiggerSnapshot/HierarchySummaries";
        if (!Directory.Exists(exportPath))
            Directory.CreateDirectory(exportPath);

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            string fileName = Path.Combine(exportPath, scene.name + "_Hierarchy.txt");
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("Scene: " + scene.name);
                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    WriteObject(writer, rootObj, 0);
                }
            }

            Debug.Log("Hierarchy exported for scene: " + scene.name);
        }
    }

    static void WriteObject(StreamWriter writer, GameObject obj, int indent)
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
}
