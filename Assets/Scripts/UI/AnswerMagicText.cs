using UnityEngine;
using System.Collections;
using System;
using TMPro;

public class AnswerMagicText : MonoBehaviour
{
    [Header("Reveal Settings")]
    public float characterRevealTime = 0.06f;
    public float overlapRatio = 0.5f;
    public float punctuationPause = 0.08f;

    [Header("Pop Effect")]
    public float popOvershoot = 1.3f;
    public float popSettlePoint = 0.4f;

    private TextMeshProUGUI tmp;
    private Coroutine revealCoroutine;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.color = Color.black;
    }

    public void Reveal(string text, Action onComplete = null)
    {
        if (tmp == null) return;

        if (revealCoroutine != null)
            StopCoroutine(revealCoroutine);

        revealCoroutine = StartCoroutine(RevealCoroutine(text, onComplete));
    }

    public void ShowInstant(string text)
    {
        if (tmp == null) return;
        Stop();
        tmp.text = text;
        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        SetAllBlack(textInfo, 255);
        ResetVertexPositions(textInfo);
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices);
    }

    public void Clear()
    {
        Stop();
        if (tmp != null)
            tmp.text = "";
    }

    public void Stop()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }
    }

    public bool IsRevealing => revealCoroutine != null;

    private IEnumerator RevealCoroutine(string text, Action onComplete)
    {
        tmp.text = text;
        yield return null;
        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        int totalChars = textInfo.characterCount;

        if (totalChars == 0)
        {
            onComplete?.Invoke();
            revealCoroutine = null;
            yield break;
        }

        Vector3[][] origVerts = new Vector3[textInfo.meshInfo.Length][];
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
            origVerts[m] = (Vector3[])textInfo.meshInfo[m].vertices.Clone();

        float[] charStartTime = new float[totalChars];
        bool[] isRevealed = new bool[totalChars];

        SetAllBlack(textInfo, 0);
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        float stepDelay = characterRevealTime * (1f - overlapRatio);
        float accumulatedPause = 0f;

        for (int i = 0; i < totalChars; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                charStartTime[i] = -1f;
                isRevealed[i] = true;
                continue;
            }

            charStartTime[i] = i * stepDelay + accumulatedPause;

            char c = textInfo.characterInfo[i].character;
            if (c == ',' || c == ';' || c == ':')
                accumulatedPause += punctuationPause * 0.5f;
            else if (c == '.' || c == '!' || c == '?')
                accumulatedPause += punctuationPause;
        }

        float totalDuration = 0f;
        for (int i = totalChars - 1; i >= 0; i--)
        {
            if (charStartTime[i] >= 0)
            {
                totalDuration = charStartTime[i] + characterRevealTime + 0.05f;
                break;
            }
        }

        float currentTime = 0f;

        while (currentTime < totalDuration)
        {
            bool anyStillRevealing = false;

            for (int i = 0; i < totalChars; i++)
            {
                if (isRevealed[i]) continue;
                if (charStartTime[i] < 0) continue;

                float t = (currentTime - charStartTime[i]) / characterRevealTime;

                if (t < 0f)
                {
                    anyStillRevealing = true;
                    continue;
                }

                t = Mathf.Clamp01(t);
                ApplyCharacterEffect(textInfo, origVerts, i, t);

                if (t >= 1f)
                    isRevealed[i] = true;
                else
                    anyStillRevealing = true;
            }

            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices);

            if (!anyStillRevealing) break;

            currentTime += Time.deltaTime;
            yield return null;
        }

        // finalize all characters
        for (int i = 0; i < totalChars; i++)
        {
            if (charStartTime[i] >= 0 && textInfo.characterInfo[i].isVisible)
                ApplyCharacterEffect(textInfo, origVerts, i, 1f);
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices);

        revealCoroutine = null;
        onComplete?.Invoke();
    }

    private void ApplyCharacterEffect(TMP_TextInfo textInfo, Vector3[][] origVerts, int charIndex, float t)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int matIndex = charInfo.materialReferenceIndex;
        int vertIndex = charInfo.vertexIndex;
        Color32[] colors = textInfo.meshInfo[matIndex].colors32;
        Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

        // fast alpha fade-in
        float alphaT = Mathf.Clamp01(t / 0.4f);
        alphaT = alphaT * alphaT;
        byte alpha = (byte)(alphaT * 255);

        Color32 col = new Color32(0, 0, 0, alpha);
        colors[vertIndex + 0] = col;
        colors[vertIndex + 1] = col;
        colors[vertIndex + 2] = col;
        colors[vertIndex + 3] = col;

        // scale punch: overshoot then settle to 1.0
        float scale;
        if (t < popSettlePoint)
        {
            float punchT = t / popSettlePoint;
            scale = Mathf.Lerp(0.5f, popOvershoot, punchT);
        }
        else
        {
            float settleT = (t - popSettlePoint) / (1f - popSettlePoint);
            scale = Mathf.Lerp(popOvershoot, 1f, settleT * settleT);
        }

        // find character center
        Vector3 orig0 = origVerts[matIndex][vertIndex + 0];
        Vector3 orig1 = origVerts[matIndex][vertIndex + 1];
        Vector3 orig2 = origVerts[matIndex][vertIndex + 2];
        Vector3 orig3 = origVerts[matIndex][vertIndex + 3];
        Vector3 center = (orig0 + orig1 + orig2 + orig3) / 4f;

        verts[vertIndex + 0] = center + (orig0 - center) * scale;
        verts[vertIndex + 1] = center + (orig1 - center) * scale;
        verts[vertIndex + 2] = center + (orig2 - center) * scale;
        verts[vertIndex + 3] = center + (orig3 - center) * scale;
    }

    private void SetAllBlack(TMP_TextInfo textInfo, byte alpha)
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[matIndex].colors32;

            Color32 col = new Color32(0, 0, 0, alpha);
            colors[vertIndex + 0] = col;
            colors[vertIndex + 1] = col;
            colors[vertIndex + 2] = col;
            colors[vertIndex + 3] = col;
        }
    }

    private void ResetVertexPositions(TMP_TextInfo textInfo)
    {
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            var meshInfo = textInfo.meshInfo[m];
            for (int v = 0; v < meshInfo.vertices.Length; v++)
                meshInfo.vertices[v] = meshInfo.vertices[v]; // force dirty
        }
    }
}