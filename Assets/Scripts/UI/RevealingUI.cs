using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class RevealingUI : BaseGameUI
{
    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        StartCoroutine(ShowSubmissionCoroutine(player, answer, onComplete, promptText));
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete)
    {
        if(player != null)
            ChangeText($"{player.playerName}'s Rising Action:");
        else
            ChangeText("The fate of the story is in your hands...");
        
        StartCoroutine(ShowOptionsCoroutine(answers, onComplete));
    }

    private IEnumerator ShowSubmissionCoroutine(Player player, string answer, Action onComplete, string promptText)
    {
        if(!string.IsNullOrEmpty(promptText))
        {
            ChangeText(promptText);
            yield return new WaitForSeconds(CalculateDisplayTime(promptText));
        }
        
        string submissionText = null;
        if(player != null) 
            submissionText = $"{player.playerName}: {answer}";
        else 
            submissionText = answer;
            
        ChangeText(submissionText);
        yield return new WaitForSeconds(CalculateDisplayTime(answer));
        
        onComplete?.Invoke();
    }

    private IEnumerator ShowOptionsCoroutine(List<string> answers, Action onComplete)
    {
        // delay for the first text to show
        yield return new WaitForSeconds(CalculateDisplayTime(displayText.text));

        foreach(string ans in answers)
        {
            if(string.IsNullOrWhiteSpace(ans))
                continue;

            ChangeText(ans);

            float displayTime = CalculateDisplayTime(ans);
            yield return new WaitForSeconds(displayTime);
        }

        ClearText();
        onComplete?.Invoke();
    }
}