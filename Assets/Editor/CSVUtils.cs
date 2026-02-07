
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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

    public static string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim('"'));
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        fields.Add(currentField.Trim('"'));
        
        return fields.ToArray();
    }
}