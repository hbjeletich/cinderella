using UnityEngine;
using System.Collections;

public class BackgroundController : MonoBehaviour
{
    public PhaseColorConfig colorConfig;

    private Material mat;
    private SpriteRenderer sr;
    private Coroutine activePlayback;

    private int _swipe1ColorId;
    private int _swipe2ColorId;
    private int _swipe3ColorId;
    private int _isSwipingId;

    private int _XSpeedId;
    private int _YSpeedId;
    private float xSpeed;
    private float ySpeed;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        _swipe1ColorId = Shader.PropertyToID("_Swipe1_Color");
        _swipe2ColorId = Shader.PropertyToID("_Swipe2_Color");
        _swipe3ColorId = Shader.PropertyToID("_Swipe3_Color");
        _isSwipingId = Shader.PropertyToID("_isSwiping");

        _XSpeedId = Shader.PropertyToID("_XSpeed");
        _YSpeedId = Shader.PropertyToID("_YSpeed");

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            mat = sr.material;
    }

    private void Start()
    {
        if (colorConfig == null || colorConfig.phases.Length == 0) return;

        var first = colorConfig.phases[0];
        mat.SetColor(_swipe1ColorId, first.gridOverlayColor);
        mat.SetColor(_swipe2ColorId, first.gridBackColor);
        mat.SetFloat(_isSwipingId, 0f);

        xSpeed = mat.GetFloat(_XSpeedId);
        ySpeed = mat.GetFloat(_YSpeedId);

        Sprite[] gridFrames = colorConfig.GetFrames(first.gridAnimationIndex);
        if (gridFrames != null && gridFrames.Length > 0)
            sr.sprite = gridFrames[gridFrames.Length - 1];
    }

    public void RunTransitionByName(string phaseName, System.Action onComplete = null)
    {
        var entry = colorConfig.GetPhase(phaseName);
        if (entry == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (activePlayback != null)
            StopCoroutine(activePlayback);

        activePlayback = StartCoroutine(RunFullTransition(entry, onComplete));
    }

    private IEnumerator RunFullTransition(PhaseColorConfig.PhaseEntry entry, System.Action onComplete)
    {
        IsTransitioning = true;
        StopSpeed();

        Sprite[] gridFrames = colorConfig.GetFrames(entry.gridAnimationIndex);
        Sprite[] wipeFrames = colorConfig.GetFrames(entry.wipeAnimationIndex);

        // ── 1. REVERSE GRID ──
        mat.SetColor(_swipe1ColorId, entry.gridOverlayColor);
        mat.SetColor(_swipe2ColorId, entry.gridBackColor);
        mat.SetFloat(_isSwipingId, 0f);

        if (gridFrames != null && gridFrames.Length > 0)
            yield return PlayFrames(gridFrames, entry.gridDuration, true);

        // ── 2. WIPE ──
        mat.SetColor(_swipe1ColorId, entry.gridBackColor);
        mat.SetColor(_swipe2ColorId, entry.transitionColor);
        mat.SetColor(_swipe3ColorId, entry.nextGridBackColor);
        mat.SetFloat(_isSwipingId, 1f);

        if (wipeFrames != null && wipeFrames.Length > 0)
            yield return PlayFrames(wipeFrames, entry.wipeDuration, false);

        // ── 3. FORWARD GRID ──
        mat.SetColor(_swipe1ColorId, entry.nextGridOverlayColor);
        mat.SetColor(_swipe2ColorId, entry.nextGridBackColor);
        mat.SetFloat(_isSwipingId, 0f);

        if (gridFrames != null && gridFrames.Length > 0)
            yield return PlayFrames(gridFrames, entry.gridDuration, false);

        StartSpeed();

        IsTransitioning = false;
        activePlayback = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayFrames(Sprite[] frames, float duration, bool reverse)
    {
        int total = frames.Length;
        float elapsed = 0f;
        int lastIndex = -1;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            int index = Mathf.Clamp(Mathf.FloorToInt(t * total), 0, total - 1);

            if (reverse)
                index = (total - 1) - index;

            if (index != lastIndex)
            {
                sr.sprite = frames[index];
                lastIndex = index;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.sprite = reverse ? frames[0] : frames[total - 1];
    }

    private void StopSpeed()
    {
        mat.SetFloat(_XSpeedId, 0f);
        mat.SetFloat(_YSpeedId, 0f);
    }

    private void StartSpeed()
    {
        mat.SetFloat(_XSpeedId, xSpeed);
        mat.SetFloat(_YSpeedId, ySpeed);
    }
}