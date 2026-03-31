#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BackgroundController))]
public class BackgroundControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BackgroundController bc = (BackgroundController)target;

        if (bc.colorConfig == null || bc.colorConfig.phases == null || bc.colorConfig.phases.Length == 0)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Phase Previews", EditorStyles.boldLabel);

        SpriteRenderer sr = bc.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            EditorGUILayout.HelpBox("No SpriteRenderer found on this object.", MessageType.Warning);
            return;
        }

        // get the material
        Material mat = Application.isPlaying ? sr.material : sr.sharedMaterial;

        if (mat == null)
        {
            EditorGUILayout.HelpBox("No material found on SpriteRenderer.", MessageType.Warning);
            return;
        }

        int swipe1 = Shader.PropertyToID("_Swipe1_Color");
        int swipe2 = Shader.PropertyToID("_Swipe2_Color");
        int isSwiping = Shader.PropertyToID("_isSwiping");

        foreach (var phase in bc.colorConfig.phases)
        {
            // show a colored swatch next to the button
            EditorGUILayout.BeginHorizontal();

            // draw small color preview boxes
            Rect rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, phase.nextGridOverlayColor);
            rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, phase.nextGridBackColor);

            string label = string.IsNullOrEmpty(phase.phaseName)
                ? "(unnamed)"
                : phase.phaseName;

            if (GUILayout.Button(label))
            {
                Undo.RecordObject(mat, "Preview Phase: " + label);

                // set to the post-transition idle look
                mat.SetColor(swipe1, phase.nextGridOverlayColor);
                mat.SetColor(swipe2, phase.nextGridBackColor);
                mat.SetFloat(isSwiping, 0f);

                // set the sprite to the final grid frame
                Sprite[] gridFrames = bc.colorConfig.GetFrames(phase.gridAnimationIndex);
                if (gridFrames != null && gridFrames.Length > 0)
                {
                    Undo.RecordObject(sr, "Preview Phase Sprite: " + label);
                    sr.sprite = gridFrames[gridFrames.Length - 1];
                }

                EditorUtility.SetDirty(mat);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        // reset to the first phase (the Start() default)
        if (GUILayout.Button("Reset to Initial"))
        {
            var first = bc.colorConfig.phases[0];
            Undo.RecordObject(mat, "Reset Background");
            mat.SetColor(swipe1, first.gridOverlayColor);
            mat.SetColor(swipe2, first.gridBackColor);
            mat.SetFloat(isSwiping, 0f);

            Sprite[] gridFrames = bc.colorConfig.GetFrames(first.gridAnimationIndex);
            if (gridFrames != null && gridFrames.Length > 0)
            {
                Undo.RecordObject(sr, "Reset Background Sprite");
                sr.sprite = gridFrames[gridFrames.Length - 1];
            }

            EditorUtility.SetDirty(mat);
            SceneView.RepaintAll();
        }
    }
}
#endif