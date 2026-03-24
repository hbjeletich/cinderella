using UnityEngine;
using System.Collections;

public class WritingUI : BaseGameUI
{
    [Header("Plot Diagram")]
    public PlotDiagram plotDiagram;

    [Header("Target fill per round (0-1)")]
    public float[] roundTargetFills = new float[] { 0.15f, 0.30f, 0.50f, 0.70f, 0.85f, 1.0f };

    private Coroutine fillCoroutine;

    protected override void Awake()
    {
        base.Awake();
        // plot diagram starts hidden and at 0
        if(plotDiagram != null)
        {
            plotDiagram.gameObject.SetActive(false);
            plotDiagram.SetFillAmount(0f);
        }
    }

    public override void Activate()
    {
        base.Activate();
        // show the plot diagram — once visible, it stays visible across phases
        if(plotDiagram != null)
            plotDiagram.gameObject.SetActive(true);
    }

    public override void Deactivate()
    {
        if(fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        // hide the plot diagram
        if(plotDiagram != null)
        {
            plotDiagram.gameObject.SetActive(false);
        }

        HideText();
    }

    public void StartFillAnimation(int roundNumber, float duration)
    {
        if(plotDiagram == null) return;

        int index = Mathf.Clamp(roundNumber - 1, 0, roundTargetFills.Length - 1);
        float targetFill = roundTargetFills[index];

        if(fillCoroutine != null)
            StopCoroutine(fillCoroutine);

        fillCoroutine = StartCoroutine(AnimateFill(targetFill, duration));
    }

    private IEnumerator AnimateFill(float targetFill, float duration)
    {
        float startFill = plotDiagram.GetFillAmount();
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // ease out so it slows down as it approaches the target
            float easedT = 1f - (1f - t) * (1f - t);
            plotDiagram.SetFillAmount(Mathf.Lerp(startFill, targetFill, easedT));
            yield return null;
        }

        plotDiagram.SetFillAmount(targetFill);
        fillCoroutine = null;
    }
}