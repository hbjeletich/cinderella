using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI canvasText;
    public float baseTextTime = 2f;
    public float timePerCharacter = 0.05f;

    public void ShowNarrative(string text, Action onComplete)
    {
        StartCoroutine(ShowNarrativeCoroutine(text, onComplete));
    }

    public void ShowSubmission(Player player, string answer, Action onComplete)
    {
        StartCoroutine(ShowSubmissionCoroutine(player, answer, onComplete));
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

        canvasText.text = "";
        onComplete?.Invoke();
    }

    private IEnumerator ShowSubmissionCoroutine(Player player, string answer, Action onComplete)
    {
        string displayText = $"{player.playerName}: {answer}";
        ChangeText(displayText);

        float displayTime = CalculateDisplayTime(answer);
        yield return new WaitForSeconds(displayTime);

        onComplete?.Invoke();
    }

    private float CalculateDisplayTime(string text)
    {
        // base time plus time per character
        float t = Mathf.Max(baseTextTime, text.Length * timePerCharacter);
        return t;
    }

    private void ChangeText(string text)
    {
        Debug.Log($"GameUI: Changing text to {text}");
        canvasText.text = text;
    }
}
