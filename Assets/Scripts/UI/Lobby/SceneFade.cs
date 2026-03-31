using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum SceneType
{
    MainMenu,
    Lobby,
    Game
}

public class SceneFade : MonoBehaviour
{
    [Header("Timing")]
    public float delayModifier = 1f; // 1 = no extra delay, 0.5 = half the max delay, etc.
    [Header("Scene Type")]
    public SceneType sceneType = SceneType.Lobby;
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError("LobbyFade: No Image component found!");
            return;
        }

        SetAlpha(0f); // start fully faded out

        SubscribeToSceneEvents();
    }

    private void SubscribeToSceneEvents()
    {
        switch (sceneType)
        {
            case SceneType.Lobby:
                LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();
                if (lobbyUI != null)
                {
                    lobbyUI.OnLobbyEntered += OnEnter;
                    lobbyUI.OnLobbyExited += OnExit;
                }
                break;
            case SceneType.MainMenu:
                MainMenuManager mainMenu = FindObjectOfType<MainMenuManager>();
                if (mainMenu != null)
                {
                    mainMenu.OnMainMenuEntered += OnEnter;
                    mainMenu.OnMainMenuExited += OnExit;
                }
                break;
            case SceneType.Game:
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.OnGameBegin += OnEnter;
                    gameManager.OnGameEnd += OnExit;
                }
                break;
        }
    }

    private void OnEnter()
    {
        Debug.Log("SceneFade: Received enter event. Starting fade in.");
        FadeIn(1f);
    }

    private void OnExit(float maxDelay)
    {
        Debug.Log($"SceneFade: Received exit event with maxDelay {maxDelay}. Starting fade out with delay modifier {delayModifier}.");
        // after the exit event, start fading out
        StartCoroutine(DelayedFadeOut(maxDelay * (1-delayModifier), maxDelay * delayModifier));
    }

    private IEnumerator DelayedFadeOut(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        FadeOut(duration);
    }

    public void FadeIn(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, duration));
    }

    public void FadeOut(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}
