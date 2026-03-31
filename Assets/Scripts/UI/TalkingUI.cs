using UnityEngine;
using System.Collections;
using System;
using TMPro;

public class TalkingUI : BaseGameUI
{
    [Header("Reveal Settings")]
    public float characterRevealTime = 0.12f;
    public float overlapRatio = 0.4f;
    public float punctuationPause = 0.15f;
    public float sentencePause = 0.8f;

    [Header("Glow")]
    public Color glowColor = new Color(1f, 0.9f, 0.5f, 1f);
    public float glowIntensity = 1.5f;

    private Coroutine revealCoroutine;

    public void ShowNarrative(string text, Action onComplete)
    {
        if (revealCoroutine != null)
            StopCoroutine(revealCoroutine);

        revealCoroutine = StartCoroutine(ShowNarrativeCoroutine(text, onComplete));
    }

    private IEnumerator ShowNarrativeCoroutine(string text, Action onComplete)
    {
        string[] separators = new string[] { ". " };
        string[] sentences = text.Split(separators, StringSplitOptions.None);

        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                continue;

            yield return RevealSentence(sentence);
            yield return new WaitForSeconds(sentencePause);
        }

        ClearText();
        onComplete?.Invoke();
    }

    private IEnumerator RevealSentence(string sentence)
    {
        displayText.gameObject.SetActive(true);
        displayText.text = sentence;
        displayText.ForceMeshUpdate();

        TMP_TextInfo textInfo = displayText.textInfo;
        int totalChars = textInfo.characterCount;

        if (totalChars == 0) yield break;

        float[] charProgress = new float[totalChars];
        float[] charStartTime = new float[totalChars];
        bool[] isRevealed = new bool[totalChars];

        SetAllAlpha(textInfo, 0);
        displayText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        float stepDelay = characterRevealTime * (1f - overlapRatio);
        float currentTime = 0f;

        for (int i = 0; i < totalChars; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                charStartTime[i] = -1f;
                isRevealed[i] = true;
                continue;
            }

            charStartTime[i] = i * stepDelay;

            char c = textInfo.characterInfo[i].character;
            if (c == ',' || c == ';' || c == ':')
                charStartTime[i] += punctuationPause * 0.5f;
        }

        // add punctuation pauses
        float accumulatedPause = 0f;
        for (int i = 0; i < totalChars; i++)
        {
            if (charStartTime[i] < 0) continue;

            charStartTime[i] += accumulatedPause;

            char c = textInfo.characterInfo[i].character;
            if (c == ',' || c == ';' || c == ':')
                accumulatedPause += punctuationPause * 0.5f;
            else if (c == '.' || c == '!' || c == '?')
                accumulatedPause += punctuationPause;
        }

        float totalDuration = charStartTime[totalChars - 1] + characterRevealTime + 0.1f;
        // find actual last visible char for duration calc
        for (int i = totalChars - 1; i >= 0; i--)
        {
            if (charStartTime[i] >= 0)
            {
                totalDuration = charStartTime[i] + characterRevealTime + 0.1f;
                break;
            }
        }

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
                charProgress[i] = t;

                ApplyCharacterEffect(textInfo, i, t);

                if (t >= 1f)
                    isRevealed[i] = true;
                else
                    anyStillRevealing = true;
            }

            displayText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            if (!anyStillRevealing) break;

            currentTime += Time.deltaTime;
            yield return null;
        }

        // snap all to final state
        for (int i = 0; i < totalChars; i++)
        {
            if (charStartTime[i] >= 0 && textInfo.characterInfo[i].isVisible)
                ApplyCharacterEffect(textInfo, i, 1f);
        }
        displayText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        yield return new WaitForSeconds(CalculateDisplayTime(sentence));
    }

    private void ApplyCharacterEffect(TMP_TextInfo textInfo, int charIndex, float t)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int matIndex = charInfo.materialReferenceIndex;
        int vertIndex = charInfo.vertexIndex;
        Color32[] colors = textInfo.meshInfo[matIndex].colors32;

        // alpha: quick sweep in during first 60%
        float alphaT = Mathf.Clamp01(t / 0.6f);
        alphaT = alphaT * alphaT;
        byte alpha = (byte)(alphaT * 255);

        // glow: peaks at t=0.3, gone by t=1.0
        float glowT = 0f;
        if (t < 0.3f)
            glowT = t / 0.3f;
        else
            glowT = 1f - ((t - 0.3f) / 0.7f);
        glowT = Mathf.Clamp01(glowT);

        // base color is white, boosted by glow
        float r = Mathf.Clamp01(1f + (glowColor.r * glowIntensity - 1f) * glowT);
        float g = Mathf.Clamp01(1f + (glowColor.g * glowIntensity - 1f) * glowT);
        float b = Mathf.Clamp01(1f + (glowColor.b * glowIntensity - 1f) * glowT);

        Color32 col = new Color32(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255),
            alpha
        );

        // stagger vertex alpha slightly for a stroke-drawing feel
        // bottom verts reveal slightly before top verts
        byte alphaTop = (byte)(Mathf.Clamp01((alphaT - 0.1f) / 0.9f) * 255);

        colors[vertIndex + 0] = new Color32(col.r, col.g, col.b, alpha);      // bottom-left
        colors[vertIndex + 1] = new Color32(col.r, col.g, col.b, alphaTop);   // top-left
        colors[vertIndex + 2] = new Color32(col.r, col.g, col.b, alphaTop);   // top-right
        colors[vertIndex + 3] = new Color32(col.r, col.g, col.b, alpha);      // bottom-right
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

    public override void Deactivate()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }
        base.Deactivate();
    }
}