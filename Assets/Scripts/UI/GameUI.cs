using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class GameUI : MonoBehaviour
{
    [Header("Sub-Controllers")]
    public TalkingUI talkingUI;
    public WritingUI writingUI;
    public RevealingUI revealingUI;
    public ScoringUI scoringUI;

    [Header("Background")]
    public BackgroundController backgroundController;

    [Header("Timer (overlay — visible across all phases)")]
    public TextMeshProUGUI timerText;

    private BaseGameUI activeController;
    private string activePhase;

    private void Start()
    {
        talkingUI?.Deactivate();
        writingUI?.Deactivate();
        revealingUI?.Deactivate();
        scoringUI?.Deactivate();
        HideTimer();
        activeController = null;
        activePhase = "Talking";
    }

    private void TransitionTo(string phaseName, BaseGameUI controller, Action onReady)
    {
        if (activeController != null && activeController != controller)
            activeController.Deactivate();

        if (activePhase != phaseName && backgroundController != null)
        {
            activePhase = phaseName;
            backgroundController.RunTransitionByName(phaseName, () =>
            {
                activeController = controller;
                activeController?.Activate();
                onReady?.Invoke();
            });
        }
        else
        {
            activePhase = phaseName;
            activeController = controller;
            activeController?.Activate();
            onReady?.Invoke();
        }
    }

    // --- Timer ---

    public void ShowTimer(int seconds)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = seconds.ToString();
        }
    }

    public void UpdateTimer(int seconds)
    {
        if (timerText != null)
            timerText.text = Mathf.Max(0, seconds).ToString();
    }

    public void HideTimer()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }

    // --- Narrative ---

    public void ShowNarrative(string text, Action onComplete)
    {
        TransitionTo("Talking", talkingUI, () =>
        {
            talkingUI.ShowNarrative(text, onComplete);
        });
    }

    // --- Writing Phase ---

    public void ShowWritingPhase(int roundNumber, float duration)
    {
        TransitionTo("Writing", writingUI, () =>
        {
            writingUI.StartFillAnimation(roundNumber, duration);
        });
    }

    // --- Reveals & Voting ---

    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        TransitionTo("Revealing", revealingUI, () =>
        {
            revealingUI.ShowSubmission(player, answer, onComplete, promptText);
        });
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete, string promptText = null)
    {
        TransitionTo("Revealing", revealingUI, () =>
        {
            revealingUI.ShowOptions(player, answers, onComplete, promptText);
        });
    }

    // --- Scoreboard ---

    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        TransitionTo("Scoring", scoringUI, () =>
        {
            scoringUI.ShowScoreboard(roundNumber, sortedPlayers, onComplete);
        });
    }
}