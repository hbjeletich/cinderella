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

    [Header("Timer (overlay — visible across all phases)")]
    public TextMeshProUGUI timerText;

    private BaseGameUI activeController;

    private void Start()
    {
        // hide all sub-controller content at start
        talkingUI?.Deactivate();
        writingUI?.Deactivate();
        revealingUI?.Deactivate();
        scoringUI?.Deactivate();
        HideTimer();
        activeController = null;
    }

    private void SetActive(BaseGameUI controller)
    {
        if(activeController != null && activeController != controller)
            activeController.Deactivate();

        activeController = controller;

        if(activeController != null)
            activeController.Activate();
    }

    // --- Timer (overlay, not tied to any sub-controller) ---

    public void ShowTimer(int seconds)
    {
        if(timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = seconds.ToString();
        }
    }

    public void UpdateTimer(int seconds)
    {
        if(timerText != null)
            timerText.text = Mathf.Max(0, seconds).ToString();
    }

    public void HideTimer()
    {
        if(timerText != null)
            timerText.gameObject.SetActive(false);
    }

    // --- Narrative (delegates to TalkingUI) ---

    public void ShowNarrative(string text, Action onComplete)
    {
        SetActive(talkingUI);
        talkingUI.ShowNarrative(text, onComplete);
    }

    // --- Writing Phase (delegates to WritingUI) ---

    public void ShowWritingPhase(int roundNumber, float duration)
    {
        SetActive(writingUI);
        writingUI.StartFillAnimation(roundNumber, duration);
    }

    // --- Reveals & Voting (delegates to RevealingUI) ---

    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        SetActive(revealingUI);
        revealingUI.ShowSubmission(player, answer, onComplete, promptText);
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete, string promptText = null)
    {
        SetActive(revealingUI);
        revealingUI.ShowOptions(player, answers, onComplete, promptText);
    }

    // --- Scoreboard (delegates to ScoringUI) ---

    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        SetActive(scoringUI);
        scoringUI.ShowScoreboard(roundNumber, sortedPlayers, onComplete);
    }
}