using UnityEngine;
using System.Collections;
using System;

public class TalkingUI : BaseGameUI
{
    public void ShowNarrative(string text, Action onComplete)
    {
        StartCoroutine(ShowNarrativeCoroutine(text, onComplete));
    }

    private IEnumerator ShowNarrativeCoroutine(string text, Action onComplete)
    {
        // split into sentences
        string[] separators = new string[] { ". " };
        string[] sentences = text.Split(separators, StringSplitOptions.None);
        
        foreach(string sentence in sentences)
        {
            if(string.IsNullOrWhiteSpace(sentence))
                continue;

            ChangeText(sentence);

            float displayTime = CalculateDisplayTime(sentence);
            yield return new WaitForSeconds(displayTime);
        }

        ClearText();
        onComplete?.Invoke();
    }
}