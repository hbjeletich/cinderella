using UnityEngine;
using System.Collections;

public class VortexProp : MonoBehaviour
{
    [Header("Vortex Center")]
    public RectTransform vortexCenter;

    [Header("Timing")]
    public float minStartDelay = 0f;
    public float maxStartDelay = 1.5f;

    public float spiralDuration = 2.5f;

    [Header("Spiral Motion")]
    public float startAngularSpeed = 90f;

    public float endAngularSpeed = 720f;

    public AnimationCurve radiusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Warp & Scale")]
    public float maxWarpStretch = 2.5f;

    public float disappearScale = 0.05f;

    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Rotation")]
    public bool spinSelf = true;
    public float selfSpinMultiplier = 1.5f;

    [Header("References")]
    public RectTransform rectTransform;

    // state
    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;
    private bool isSpiralActive = false;

    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        baseScale = rectTransform.localScale;
        baseAnchoredPos = rectTransform.anchoredPosition;

        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
        if (lobbyUI != null)
        {
            lobbyUI.OnLobbyEntered += OnLobbyEnter;
            lobbyUI.OnLobbyExited += OnLobbyExit;
        }
    }

    private void OnLobbyEnter()
    {
        ResetProp();
    }

    private void OnLobbyExit(float maxDelay)
    {
        if (!isSpiralActive)
        {
            StartCoroutine(VortexRoutine());
        }
    }

    private IEnumerator VortexRoutine()
    {
        isSpiralActive = true;

        // stagger the start so props don't all go at once
        float delay = Random.Range(minStartDelay, maxStartDelay);
        yield return new WaitForSeconds(delay);

        Debug.Log($"VortexProp: {gameObject.name} beginning spiral.");

        // use world position so anchors don't matter
        Vector3 startWorldPos = rectTransform.position;
        Vector3 centerWorldPos = vortexCenter != null
            ? vortexCenter.position
            : rectTransform.root.GetComponent<RectTransform>().position;

        // initial offset from center in world space
        Vector2 offset = new Vector2(
            startWorldPos.x - centerWorldPos.x,
            startWorldPos.y - centerWorldPos.y
        );
        float startRadius = offset.magnitude;
        float startAngle = Mathf.Atan2(offset.y, offset.x);

        float elapsed = 0f;
        float currentAngle = startAngle;

        while (elapsed < spiralDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spiralDuration);

            // --- RADIUS: shrink toward center ---
            float radiusT = radiusCurve.Evaluate(t);
            float currentRadius = Mathf.Lerp(startRadius, 0f, radiusT);

            // --- ANGULAR VELOCITY: accelerate the spin ---
            float angularSpeed = Mathf.Lerp(startAngularSpeed, endAngularSpeed, t);
            currentAngle += angularSpeed * Mathf.Deg2Rad * Time.deltaTime;

            // --- POSITION (world space) ---
            float x = centerWorldPos.x + Mathf.Cos(currentAngle) * currentRadius;
            float y = centerWorldPos.y + Mathf.Sin(currentAngle) * currentRadius;
            rectTransform.position = new Vector3(x, y, rectTransform.position.z);

            // --- SCALE: shrink down ---
            float scaleT = scaleCurve.Evaluate(t);
            float uniformScale = Mathf.Lerp(1f, 0f, scaleT);

            // --- WARP: stretch along the tangent direction ---
            float warpAmount = Mathf.Lerp(1f, maxWarpStretch, t * t);
            float warpX = uniformScale * warpAmount;
            float warpY = uniformScale / Mathf.Sqrt(warpAmount);

            rectTransform.localScale = new Vector3(
                baseScale.x * warpX,
                baseScale.y * warpY,
                baseScale.z
            );

            // --- SELF ROTATION: spin the prop itself ---
            if (spinSelf)
            {
                float selfSpin = angularSpeed * selfSpinMultiplier * Time.deltaTime;
                rectTransform.Rotate(0f, 0f, selfSpin);
            }

            // --- EARLY EXIT: if scale is tiny, just disappear ---
            if (uniformScale <= disappearScale)
            {
                Debug.Log($"VortexProp: {gameObject.name} consumed by vortex.");
                break;
            }

            yield return null;
        }

        // snap to center and deactivate
        rectTransform.position = centerWorldPos;
        rectTransform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        Debug.Log($"VortexProp: {gameObject.name} deactivated.");
        isSpiralActive = false;
    }

    public void ResetProp()
    {
        StopAllCoroutines();
        isSpiralActive = false;

        gameObject.SetActive(true);
        rectTransform.anchoredPosition = baseAnchoredPos;
        rectTransform.localScale = baseScale;
        rectTransform.localRotation = Quaternion.identity;
    }
}