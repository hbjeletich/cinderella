using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TitleAnimator : MonoBehaviour
{
    [Header("Title Images")]
    public Sprite[] titleSprites;

    [Header("Timing")]
    public float displayDuration = 3f;
    public float spinDuration = 0.4f;

    [Header("Idle Animation")]
    public float wobbleAngle = 8f;
    public float wobbleSpeed = 2.5f;
    public float breatheAmount = 0.04f;
    public float breatheSpeed = 1.8f;
    public float bounceMagnitude = 6f;
    public float bounceSpeed = 2.2f;

    [Header("Spin Transition")]
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float spinSquashAmount = 0.3f;

    private RectTransform rect;
    private Image displayImage;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;
    private int currentIndex = 0;
    private bool isSpinning = false;
    private Coroutine cycleCoroutine;
    private bool canIdle = true;

    void Awake()
    {
        // subscribe to lobby events to start/stop animation
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)        
        {
            lobbyUI.OnLobbyEntered += StartAnimation;
            lobbyUI.OnLobbyExited += StopAnimation;
        }
    }
    void Start()
    {
        rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("TitleAnimator: No RectTransform found!");
            return;
        }

        displayImage = GetComponent<Image>();

        baseScale = rect.localScale;
        baseAnchoredPos = rect.anchoredPosition;

        if (titleSprites != null && titleSprites.Length > 0)
        {
            displayImage.sprite = titleSprites[0];
            cycleCoroutine = StartCoroutine(CycleRoutine());
        }
    }

    public void StopAnimation(float maxDelay)
    {
        if (cycleCoroutine != null)
            StopCoroutine(cycleCoroutine);

        isSpinning = false;
        canIdle = false;
    }

    public void StartAnimation()
    {
        if (cycleCoroutine == null)
            cycleCoroutine = StartCoroutine(CycleRoutine());

        isSpinning = false;
        canIdle = true;
    }

    void Update()
    {
        if (isSpinning || titleSprites == null || titleSprites.Length == 0)
            return;

        if(canIdle) ApplyIdleAnimation();
    }

    private void ApplyIdleAnimation()
    {
        float t = Time.time;

        // wobble rotation
        float wobble = Mathf.Sin(t * wobbleSpeed) * wobbleAngle;
        rect.localRotation = Quaternion.Euler(0f, 0f, wobble);

        // breathe scale
        float breathe = 1f + Mathf.Sin(t * breatheSpeed) * breatheAmount;
        rect.localScale = baseScale * breathe;

        // little vertical bounce (offset sine so it doesn't sync perfectly with wobble)
        float bounce = Mathf.Sin(t * bounceSpeed + 0.7f) * bounceMagnitude;
        rect.anchoredPosition = baseAnchoredPos + new Vector2(0f, bounce);
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            // idle for a while
            yield return new WaitForSeconds(displayDuration);

            if (titleSprites.Length <= 1)
                continue;

            // spin out → swap → spin in
            yield return StartCoroutine(SpinSwap());
        }
    }

    private IEnumerator SpinSwap()
    {
        isSpinning = true;

        // reset idle transforms before spinning
        rect.localRotation = Quaternion.identity;
        rect.localScale = baseScale;
        rect.anchoredPosition = baseAnchoredPos;

        float half = spinDuration * 0.5f;

        // spin out
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float curved = spinCurve.Evaluate(t);

            // Y rotation from 0 to 90
            float yRot = Mathf.Lerp(0f, 90f, curved);
            rect.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // squash the X scale so it feels like it's turning edge-on
            float xScale = Mathf.Lerp(1f, spinSquashAmount, curved);
            rect.localScale = new Vector3(baseScale.x * xScale, baseScale.y, baseScale.z);

            yield return null;
        }

        // swap sprite
        currentIndex = (currentIndex + 1) % titleSprites.Length;
        displayImage.sprite = titleSprites[currentIndex];

        // spin in
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            float curved = spinCurve.Evaluate(t);

            float yRot = Mathf.Lerp(90f, 0f, curved);
            rect.localRotation = Quaternion.Euler(0f, yRot, 0f);

            float xScale = Mathf.Lerp(spinSquashAmount, 1f, curved);
            rect.localScale = new Vector3(baseScale.x * xScale, baseScale.y, baseScale.z);

            yield return null;
        }

        // snap clean
        rect.localRotation = Quaternion.identity;
        rect.localScale = baseScale;
        rect.anchoredPosition = baseAnchoredPos;

        // little landing pop
        yield return StartCoroutine(LandingPop());

        isSpinning = false;
    }

    private IEnumerator LandingPop()
    {
        // overshoot scale then settle
        float popDuration = 0.25f;
        float overshoot = 1.12f;
        float undershoot = 0.96f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);

            float scale;
            if (t < 0.4f)
            {
                // overshoot
                float sub = t / 0.4f;
                scale = Mathf.Lerp(1f, overshoot, sub);
            }
            else if (t < 0.7f)
            {
                // undershoot
                float sub = (t - 0.4f) / 0.3f;
                scale = Mathf.Lerp(overshoot, undershoot, sub);
            }
            else
            {
                // settle
                float sub = (t - 0.7f) / 0.3f;
                scale = Mathf.Lerp(undershoot, 1f, sub);
            }

            rect.localScale = baseScale * scale;
            yield return null;
        }

        rect.localScale = baseScale;
    }
}