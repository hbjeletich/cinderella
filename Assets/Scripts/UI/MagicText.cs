using UnityEngine;
using System.Collections;
using System;
using TMPro;

public class MagicText : MonoBehaviour
{
    [Header("Reveal Settings")]
    public float characterRevealTime = 0.12f;
    public float overlapRatio = 0.4f;
    public float punctuationPause = 0.15f;

    [Header("Glow")]
    public Color glowColor = new Color(1f, 0.9f, 0.5f, 1f);
    public float glowIntensity = 1.5f;

    private TextMeshProUGUI tmp;
    private Coroutine revealCoroutine;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
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
        SetAllAlpha(tmp.textInfo, 255);
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
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
        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        int totalChars = textInfo.characterCount;

        if (totalChars == 0)
        {
            onComplete?.Invoke();
            revealCoroutine = null;
            yield break;
        }

        float[] charStartTime = new float[totalChars];
        bool[] isRevealed = new bool[totalChars];

        SetAllAlpha(textInfo, 0);
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
                totalDuration = charStartTime[i] + characterRevealTime + 0.1f;
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
                ApplyCharacterEffect(textInfo, i, t);

                if (t >= 1f)
                    isRevealed[i] = true;
                else
                    anyStillRevealing = true;
            }

            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            if (!anyStillRevealing) break;

            currentTime += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < totalChars; i++)
        {
            if (charStartTime[i] >= 0 && textInfo.characterInfo[i].isVisible)
                ApplyCharacterEffect(textInfo, i, 1f);
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        revealCoroutine = null;
        onComplete?.Invoke();
    }

    private void ApplyCharacterEffect(TMP_TextInfo textInfo, int charIndex, float t)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int matIndex = charInfo.materialReferenceIndex;
        int vertIndex = charInfo.vertexIndex;
        Color32[] colors = textInfo.meshInfo[matIndex].colors32;

        float alphaT = Mathf.Clamp01(t / 0.6f);
        alphaT = alphaT * alphaT;
        byte alpha = (byte)(alphaT * 255);

        float glowT = 0f;
        if (t < 0.3f)
            glowT = t / 0.3f;
        else
            glowT = 1f - ((t - 0.3f) / 0.7f);
        glowT = Mathf.Clamp01(glowT);

        float r = Mathf.Clamp01(1f + (glowColor.r * glowIntensity - 1f) * glowT);
        float g = Mathf.Clamp01(1f + (glowColor.g * glowIntensity - 1f) * glowT);
        float b = Mathf.Clamp01(1f + (glowColor.b * glowIntensity - 1f) * glowT);

        Color32 col = new Color32(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255),
            alpha
        );

        byte alphaTop = (byte)(Mathf.Clamp01((alphaT - 0.1f) / 0.9f) * 255);

        colors[vertIndex + 0] = new Color32(col.r, col.g, col.b, alpha);
        colors[vertIndex + 1] = new Color32(col.r, col.g, col.b, alphaTop);
        colors[vertIndex + 2] = new Color32(col.r, col.g, col.b, alphaTop);
        colors[vertIndex + 3] = new Color32(col.r, col.g, col.b, alpha);
    }

    private void SetAllAlpha(TMP_TextInfo textInfo, byte alpha)
    {
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[matIndex].colors32;

            Color32 col = new Color32(255, 255, 255, alpha);
            colors[vertIndex + 0] = col;
            colors[vertIndex + 1] = col;
            colors[vertIndex + 2] = col;
            colors[vertIndex + 3] = col;
        }
    }
}