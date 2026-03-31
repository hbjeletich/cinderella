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

    [Header("Containers")]
    public GameObject talkingContainer;
    public GameObject writingContainer;
    public GameObject revealingContainer;
    public GameObject scoringContainer;

    [Header("Background")]
    public BackgroundController backgroundController;

    [Header("Timer (overlay — visible across all phases)")]
    public TextMeshProUGUI timerText;
    public GameObject timerContainer;

    private BaseGameUI activeController;
    private string activePhase;
    private bool isTransitioning;
    private int pendingTimerSeconds = -1;

    private void Start()
    {
        talkingUI?.Deactivate();
        writingUI?.Deactivate();
        revealingUI?.Deactivate();
        scoringUI?.Deactivate();
        HideTimer();
        activeController = null;
        activePhase = "Talking";

        ShowContainer(talkingContainer);
    }

    private void TransitionTo(string phaseName, BaseGameUI controller, Action onReady)
    {
        HideTimer();
        pendingTimerSeconds = -1;

        if (activePhase == phaseName)
        {
            activeController = controller;
            activeController?.Activate();
            onReady?.Invoke();
            return;
        }

        bool leavingWriting = activeController is WritingUI;

        Action startBackgroundTransition = () =>
        {
            if (activeController != null && activeController != controller)
                activeController.Deactivate();

            if (backgroundController != null)
            {
                isTransitioning = true;
                activePhase = phaseName;
                backgroundController.RunTransitionByName(phaseName, () =>
                {
                    isTransitioning = false;
                    activeController = controller;
                    activeController?.Activate();

                    if (pendingTimerSeconds > 0)
                        ShowTimerImmediate(pendingTimerSeconds);

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
        };

        if (leavingWriting)
        {
            ((WritingUI)activeController).SlideOut(() => startBackgroundTransition());
        }
        else
        {
            startBackgroundTransition();
        }
    }

    // --- Timer ---

    public void ShowTimer(int seconds)
    {
        if (isTransitioning)
        {
            pendingTimerSeconds = seconds;
            return;
        }
        ShowTimerImmediate(seconds);
    }

    private void ShowTimerImmediate(int seconds)
    {
        if (timerContainer != null && timerText != null)
        {
            timerContainer.SetActive(true);
            timerText.text = seconds.ToString();
        }
    }

    public void UpdateTimer(int seconds)
    {
        if (isTransitioning)
        {
            pendingTimerSeconds = Mathf.Max(0, seconds);
            return;
        }
        if (timerText != null)
            timerText.text = Mathf.Max(0, seconds).ToString();
    }

    public void HideTimer()
    {
        if (timerContainer != null)
            timerContainer.SetActive(false);
    }

    public void ShowContainer(GameObject container)
    {
        if (container != null)
        {
            talkingContainer.SetActive(container == talkingContainer);
            writingContainer.SetActive(container == writingContainer);
            revealingContainer.SetActive(container == revealingContainer);
            scoringContainer.SetActive(container == scoringContainer);
        }
    }

    // --- Narrative ---

    public void ShowNarrative(string text, Action onComplete)
    {
        TransitionTo("Talking", talkingUI, () =>
        {
            ShowContainer(talkingContainer);
            talkingUI.ShowNarrative(text, onComplete);
        });
    }

    // --- Writing Phase ---

    public void ShowWritingPhase(int roundNumber, float duration)
    {
        TransitionTo("Writing", writingUI, () =>
        {
            ShowContainer(writingContainer);
            writingUI.StartFillAnimation(roundNumber, duration);
        });
    }

    // --- Reveals & Voting ---

    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        TransitionTo("Revealing", revealingUI, () =>
        {
            ShowContainer(revealingContainer);
            revealingUI.ShowSubmission(player, answer, onComplete, promptText);
        });
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete, string promptText = null)
    {
        TransitionTo("Revealing", revealingUI, () =>
        {
            ShowContainer(revealingContainer);
            revealingUI.ShowOptions(player, answers, onComplete, promptText);
        });
    }

    // --- Scoreboard ---

    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        TransitionTo("Scoring", scoringUI, () =>
        {
            ShowContainer(scoringContainer);
            scoringUI.ShowScoreboard(roundNumber, sortedPlayers, onComplete);
        });
    }
}