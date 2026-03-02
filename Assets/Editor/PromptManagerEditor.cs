using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PromptManager))]
public class PromptManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PromptManager promptManager = (PromptManager)target;
        if (GUILayout.Button("Load Prompts"))
        {
            promptManager.ClearPrompts();
            promptManager.LoadPrompts();
        }
    }
}
