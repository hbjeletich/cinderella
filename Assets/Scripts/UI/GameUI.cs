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

    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        StartCoroutine(ShowSubmissionCoroutine(player, answer, onComplete, promptText));
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete)
    {
        if(player != null)
            canvasText.text = $"{player.playerName}'s Rising Action:";
        else
            canvasText.text = "The fate of the story is in your hands...";
        
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

    private IEnumerator ShowSubmissionCoroutine(Player player, string answer, Action onComplete, string promptText)
    {
        
        if(!string.IsNullOrEmpty(promptText))
        {
            ChangeText(promptText);
            yield return new WaitForSeconds(CalculateDisplayTime(promptText));
        }
        
        string displayText = null;
        if(player != null) 
            displayText = $"{player.playerName}: {answer}";
        else 
            displayText = answer;
            
        ChangeText(displayText);
        yield return new WaitForSeconds(CalculateDisplayTime(answer));
        
        onComplete?.Invoke();
    }

    private IEnumerator ShowOptionsCoroutine(List<string> answers, Action onComplete)
    {
        // delay for the first text to show
        yield return new WaitForSeconds(CalculateDisplayTime(canvasText.text));

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

    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        StartCoroutine(ShowScoreboardCoroutine(roundNumber, sortedPlayers, onComplete));
    }

    private IEnumerator ShowScoreboardCoroutine(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        string header = $"Round {roundNumber} Complete!";
        ChangeText(header);
        yield return new WaitForSeconds(baseTextTime);

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            int place = sortedPlayers.Count - i;
            string label = GetOrdinal(place);
            Player player = sortedPlayers[i];
            string line = $"{label}: {player.playerName} — {player.score} pts";
            ChangeText(line);
            yield return new WaitForSeconds(CalculateDisplayTime(line));
        }

        canvasText.text = "";
        onComplete?.Invoke();
    }

    private string GetOrdinal(int n)
    {
        // im sure theres a better way to do this
        // but it works for the small player count!
        string finalString = "";
        switch (n)
        {
            case 1:
                finalString = $"{n}st";
                break;
            case 2:
                finalString = $"{n}nd";
                break;
            case 3:
                finalString = $"{n}rd";
                break;
            default:
                finalString = $"{n}th";
                break;
        }
        return finalString;
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
