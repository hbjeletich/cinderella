using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LobbyMovingProp : MonoBehaviour
{
    [Header("Positions")]
    public Vector3 startingPosition;
    public Vector3 onscreenPosition;
    public Vector3 endingPosition;

    [Header("Movement Settings -- Idle")]
    public float idleAmplitude = 0.5f;
    public float idleFrequency = 1f;

    [Header("Animation")]
    public Animator animator;
    public string enterAnimationName = "Enter";
    public string idleAnimationName = "Idle";
    public string exitAnimationName = "Exit";

    [Header("References")]
    public RectTransform rectTransform;

    private Vector3 targetPosition;
    private bool isIdle = false;

    private float enterDuration;
    private float exitDuration;

    void Start()
    {
        // subscribe to OnLobbyEntered event to trigger entrance animation
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();

        if (lobbyUI != null)
        {
            lobbyUI.OnLobbyEntered += EnterScreen;
            lobbyUI.OnLobbyExited += ExitScreen;
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = startingPosition;
        targetPosition = onscreenPosition;

        // get animation clip lengths for timing
        if (animator != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == enterAnimationName)
                    enterDuration = clip.length;
                else if (clip.name == exitAnimationName)
                    exitDuration = clip.length;
            }
        }
    }

    public void EnterScreen()
    {
        isIdle = false;
        targetPosition = onscreenPosition;

        StartCoroutine(EnterRoutine());
    }

    void Update()
    {
        if (isIdle)
        {
            // apply idle floating effect
            float idleOffset = Mathf.Sin(Time.time * idleFrequency) * idleAmplitude;
            rectTransform.anchoredPosition = targetPosition + new Vector3(0, idleOffset, 0);
        }
    }

    public void ExitScreen(float maxDelay)
    {
        Debug.Log("LobbyMovingProp: ExitScreen called with maxDelay = " + maxDelay);
        isIdle = false;
        targetPosition = endingPosition;

        animator.SetTrigger(exitAnimationName);

        // ensure coroutine duration does not exceed maxDelay
        if(exitDuration > maxDelay)
        {
            exitDuration = maxDelay;
        }

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        if (animator != null)
        {
            animator.SetTrigger(enterAnimationName);
        }

        // move to onscreen position over animation duration
        float elapsed = 0f;
        Vector3 startPos = rectTransform.anchoredPosition;

        while (elapsed < enterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / enterDuration);
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;

        isIdle = true;
    }

    private IEnumerator ExitRoutine()
    {
        // move to offscreen position over animation duration
        float elapsed = 0f;
        Vector3 startPos = rectTransform.anchoredPosition;

        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }
}
