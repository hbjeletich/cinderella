using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class WritingUI : BaseGameUI
{
    [Header("Containers")]
    public RectTransform timerContainer;
    public RectTransform plotDiagramContainer;

    [Header("Plot Diagram")]
    public PlotDiagram plotDiagram;

    [Header("Submission Counter")]
    public TextMeshProUGUI submissionCounterText;

    [Header("Target fill per round (0-1)")]
    public float[] roundTargetFills = new float[] { 0.15f, 0.30f, 0.50f, 0.70f, 0.85f, 1.0f };

    [Header("Animation")]
    public float dropDuration = 0.5f;
    public float dropOvershoot = 1.3f;
    public float staggerDelay = 0.2f;
    public float exitDuration = 0.3f;

    private Coroutine fillCoroutine;
    private Coroutine counterCoroutine;
    private float timerStartY;
    private float plotStartY;
    private bool hasStoredPositions;
    private int currentRound;

    protected override void Awake()
    {
        base.Awake();

        if (plotDiagram != null)
        {
            plotDiagram.gameObject.SetActive(false);
            plotDiagram.SetFillAmount(0f);
        }

        if (submissionCounterText != null)
            submissionCounterText.gameObject.SetActive(false);
    }

    private void StorePositions()
    {
        if (hasStoredPositions) return;
        if (timerContainer != null) timerStartY = timerContainer.anchoredPosition.y;
        if (plotDiagramContainer != null) plotStartY = plotDiagramContainer.anchoredPosition.y;
        hasStoredPositions = true;
    }

    public override void Activate()
    {
        base.Activate();
        StorePositions();

        if (plotDiagram != null)
            plotDiagram.gameObject.SetActive(true);

        if (submissionCounterText != null)
        {
            submissionCounterText.gameObject.SetActive(true);
            UpdateSubmissionCounter(0, PlayerManager.Instance.GetPlayerCount());
        }

        DropIn();
    }

    public override void Deactivate()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        if (counterCoroutine != null)
        {
            StopCoroutine(counterCoroutine);
            counterCoroutine = null;
        }

        if (hasStoredPositions)
        {
            timerContainer?.DOKill();
            plotDiagramContainer?.DOKill();

            if (timerContainer != null)
                timerContainer.anchoredPosition = new Vector2(timerContainer.anchoredPosition.x, timerStartY);
            if (plotDiagramContainer != null)
                plotDiagramContainer.anchoredPosition = new Vector2(plotDiagramContainer.anchoredPosition.x, plotStartY);
        }

        if (plotDiagram != null)
            plotDiagram.gameObject.SetActive(false);
        if (submissionCounterText != null)
            submissionCounterText.gameObject.SetActive(false);

        HideText();
    }

    public void StartFillAnimation(int roundNumber, float duration)
    {
        if (plotDiagram == null) return;

        currentRound = roundNumber;
        int index = Mathf.Clamp(roundNumber - 1, 0, roundTargetFills.Length - 1);
        float targetFill = roundTargetFills[index];

        if (fillCoroutine != null)
            StopCoroutine(fillCoroutine);

        fillCoroutine = StartCoroutine(AnimateFill(targetFill, duration));

        if (counterCoroutine != null)
            StopCoroutine(counterCoroutine);

        counterCoroutine = StartCoroutine(PollSubmissions(duration));
    }

    public void SlideOut(TweenCallback onComplete = null)
    {
        float offscreenY = Screen.height;

        Sequence seq = DOTween.Sequence();

        if (plotDiagramContainer != null)
            seq.Append(plotDiagramContainer.DOAnchorPosY(offscreenY, exitDuration).SetEase(Ease.InBack));

        if (timerContainer != null)
            seq.Append(timerContainer.DOAnchorPosY(offscreenY, exitDuration).SetEase(Ease.InBack));

        if (onComplete != null)
            seq.OnComplete(onComplete);
    }

    private void DropIn()
    {
        float offscreenY = Screen.height;

        if (timerContainer != null)
        {
            timerContainer.DOKill();
            timerContainer.anchoredPosition = new Vector2(timerContainer.anchoredPosition.x, offscreenY);
            timerContainer.DOAnchorPosY(timerStartY, dropDuration)
                .SetEase(Ease.OutBack, dropOvershoot);
        }

        if (plotDiagramContainer != null)
        {
            plotDiagramContainer.DOKill();
            plotDiagramContainer.anchoredPosition = new Vector2(plotDiagramContainer.anchoredPosition.x, offscreenY);
            plotDiagramContainer.DOAnchorPosY(plotStartY, dropDuration)
                .SetEase(Ease.OutBack, dropOvershoot)
                .SetDelay(staggerDelay);
        }
    }

    private IEnumerator AnimateFill(float targetFill, float duration)
    {
        float startFill = plotDiagram.GetFillAmount();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - (1f - t) * (1f - t);
            plotDiagram.SetFillAmount(Mathf.Lerp(startFill, targetFill, easedT));
            yield return null;
        }

        plotDiagram.SetFillAmount(targetFill);
        fillCoroutine = null;
    }

    private IEnumerator PollSubmissions(float duration)
    {
        int totalPlayers = PlayerManager.Instance.GetPlayerCount();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            int submitted = 0;
            foreach (Player p in PlayerManager.Instance.players)
            {
                if (p.hasSubmittedThisRound) submitted++;
            }
            UpdateSubmissionCounter(submitted, totalPlayers);
            elapsed += Time.deltaTime;
            yield return null;
        }

        UpdateSubmissionCounter(totalPlayers, totalPlayers);
        counterCoroutine = null;
    }

    private void UpdateSubmissionCounter(int submitted, int total)
    {
        if (submissionCounterText != null)
            submissionCounterText.text = $"{submitted} of {total} submitted";
    }
}