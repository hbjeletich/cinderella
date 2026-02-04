
using UnityEngine;
using UnityEditor;
public static class CSVUtils
{
    public static string GetSelectedPath()
    {
        string path = "Assets";

        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
        }
        return path;
    }
}