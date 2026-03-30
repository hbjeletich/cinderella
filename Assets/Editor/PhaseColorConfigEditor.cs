using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(PhaseColorConfig))]
public class PhaseColorConfigEditor : Editor
{
    private SerializedProperty animationsProp;
    private SerializedProperty phasesProp;

    private void OnEnable()
    {
        animationsProp = serializedObject.FindProperty("animations");
        phasesProp = serializedObject.FindProperty("phases");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Transition Animations", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select sprites in the Project window, then click 'Populate from selection'. " +
            "Sorted by natural name order.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        int newSize = EditorGUILayout.IntField("Size", animationsProp.arraySize);
        if (newSize != animationsProp.arraySize)
            animationsProp.arraySize = Mathf.Max(0, newSize);

        EditorGUI.indentLevel++;
        for (int i = 0; i < animationsProp.arraySize; i++)
        {
            SerializedProperty animProp = animationsProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = animProp.FindPropertyRelative("animationName");
            SerializedProperty framesProp = animProp.FindPropertyRelative("frames");

            string label = string.IsNullOrEmpty(nameProp.stringValue)
                ? $"Animation {i}" : nameProp.stringValue;

            animProp.isExpanded = EditorGUILayout.Foldout(animProp.isExpanded, $"[{i}] {label}", true);

            if (animProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nameProp);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Frames: {framesProp.arraySize}", GUILayout.Width(80));

                Sprite[] selected = GetSelectedSprites();
                GUI.enabled = selected.Length > 0;
                if (GUILayout.Button(selected.Length > 0
                    ? $"Populate from selection ({selected.Length})"
                    : "Populate from selection (none selected)"))
                {
                    Undo.RecordObject(target, "Populate Animation Frames");
                    PopulateFrames(framesProp, selected);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                if (framesProp.arraySize > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(target, "Clear Frames");
                        framesProp.ClearArray();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.PropertyField(framesProp, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(2);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Phase Definitions", EditorStyles.boldLabel);

        if (animationsProp.arraySize > 0)
        {
            string list = "";
            for (int i = 0; i < animationsProp.arraySize; i++)
            {
                var n = animationsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("animationName").stringValue;
                list += $"  [{i}] {(string.IsNullOrEmpty(n) ? "(unnamed)" : n)}\n";
            }
            EditorGUILayout.HelpBox("Animation index reference:\n" + list, MessageType.None);
        }

        EditorGUILayout.PropertyField(phasesProp, true);
        serializedObject.ApplyModifiedProperties();
    }

    private Sprite[] GetSelectedSprites()
    {
        var sprites = Selection.objects.OfType<Sprite>().ToList();

        foreach (var tex in Selection.objects.OfType<Texture2D>())
        {
            string path = AssetDatabase.GetAssetPath(tex);
            sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
        }

        return sprites.OrderBy(s => s.name, new NaturalStringComparer()).ToArray();
    }

    private void PopulateFrames(SerializedProperty framesProp, Sprite[] sprites)
    {
        framesProp.ClearArray();
        for (int i = 0; i < sprites.Length; i++)
        {
            framesProp.InsertArrayElementAtIndex(i);
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    private class NaturalStringComparer : System.Collections.Generic.IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (a == null || b == null) return string.Compare(a, b);
            int ia = 0, ib = 0;
            while (ia < a.Length && ib < b.Length)
            {
                if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
                {
                    long numA = 0, numB = 0;
                    while (ia < a.Length && char.IsDigit(a[ia])) numA = numA * 10 + (a[ia++] - '0');
                    while (ib < b.Length && char.IsDigit(b[ib])) numB = numB * 10 + (b[ib++] - '0');
                    if (numA != numB) return numA.CompareTo(numB);
                }
                else
                {
                    int cmp = char.ToLowerInvariant(a[ia]).CompareTo(char.ToLowerInvariant(b[ib]));
                    if (cmp != 0) return cmp;
                    ia++; ib++;
                }
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}