using UnityEngine;
using System.Collections;
using System;

public class TalkingUI : BaseGameUI
{
    [Header("Timing")]
    public float sentencePause = 0.8f;

    private MagicText magicText;
    private Coroutine narrativeCoroutine;

    protected override void Awake()
    {
        base.Awake();
        if (displayText != null)
            magicText = displayText.GetComponent<MagicText>();
    }

    public void ShowNarrative(string text, Action onComplete)
    {
        if (narrativeCoroutine != null)
            StopCoroutine(narrativeCoroutine);

        narrativeCoroutine = StartCoroutine(ShowNarrativeCoroutine(text, onComplete));
    }

    private IEnumerator ShowNarrativeCoroutine(string text, Action onComplete)
    {
        displayText.gameObject.SetActive(true);

        string[] separators = new string[] { ". " };
        string[] sentences = text.Split(separators, StringSplitOptions.None);

        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                continue;

            bool revealDone = false;
            magicText.Reveal(sentence, () => revealDone = true);
            yield return new WaitUntil(() => revealDone);

            yield return new WaitForSeconds(CalculateDisplayTime(sentence));
        }

        magicText.Clear();
        onComplete?.Invoke();
        narrativeCoroutine = null;
    }

    public override void Deactivate()
    {
        if (narrativeCoroutine != null)
        {
            StopCoroutine(narrativeCoroutine);
            narrativeCoroutine = null;
        }
        magicText?.Stop();
        base.Deactivate();
    }
}