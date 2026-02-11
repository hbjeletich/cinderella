using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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

    public void ShowOptions(Player player, List<string> answers, Action onComplete)
    {
        answers.Insert(0, $"{player.playerName}'s Rising Action:");
        StartCoroutine(ShowOptionsCoroutine(answers, onComplete));
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

    private IEnumerator ShowOptionsCoroutine(List<string> answers, Action onComplete)
    {
        foreach(string ans in answers)
        {
            if(string.IsNullOrWhiteSpace(ans))
                continue;

            ChangeText(ans);

            float displayTime = CalculateDisplayTime(ans);
            yield return new WaitForSeconds(displayTime);
        }

        canvasText.text = "";
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
