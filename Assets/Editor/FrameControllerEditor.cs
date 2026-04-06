#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(FrameController))]
public class FrameControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FrameController fc = (FrameController)target;

        if (fc.colorConfig == null || fc.colorConfig.phases == null || fc.colorConfig.phases.Length == 0)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Frame Phase Previews", EditorStyles.boldLabel);

        Image image = fc.GetComponent<Image>();

        Material mat = image.material;

        int frameAccent = Shader.PropertyToID("_FrameAccentColor");
        int frameMain = Shader.PropertyToID("_FrameColor");
        int frameDetail = Shader.PropertyToID("_DetailColor");
        int frameFineDetail = Shader.PropertyToID("_CenterDetailColor");

        foreach (var phase in fc.colorConfig.phases)
        {
            EditorGUILayout.BeginHorizontal();

            Rect rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, phase.mainColor);
            rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, phase.accentColor);
            rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, phase.detailColor);
            rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, phase.fineDetailColor);

            string label;
            if(string.IsNullOrEmpty(phase.phaseName))
            {
                label = "(unnamed)";
            }
            else
            {
                label = phase.phaseName;
            }


            if (GUILayout.Button(label))
            {
                Undo.RecordObject(mat, "Preview Frame Phase: " + label);

                mat.SetColor(frameMain, phase.mainColor);
                mat.SetColor(frameAccent, phase.accentColor);
                mat.SetColor(frameDetail, phase.detailColor);
                mat.SetColor(frameFineDetail, phase.fineDetailColor);

                EditorUtility.SetDirty(mat);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Reset to Initial"))
        {
            var first = fc.colorConfig.phases[0];
            Undo.RecordObject(mat, "Reset Frame");

            mat.SetColor(frameMain, first.mainColor);
            mat.SetColor(frameAccent, first.accentColor);
            mat.SetColor(frameDetail, first.detailColor);
            mat.SetColor(frameFineDetail, first.fineDetailColor);

            EditorUtility.SetDirty(mat);
            SceneView.RepaintAll();
        }
    }
}
#endif