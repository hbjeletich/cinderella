using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    
    [Header("Settings Menu")]
    public GameObject settingsMenu;
    public Button settingsCloseButton;

    [Header("Scene Fade Time")]
    public float fadeOutDelay = 1f;

    public Action OnMainMenuEntered;
    public Action<float> OnMainMenuExited;

    private void Awake()
    {
        OnMainMenuEntered?.Invoke();
    }

    private void Start()
    {
        settingsMenu.SetActive(false);

        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        settingsCloseButton.onClick.AddListener(() => ToggleSettingsMenu(false));
    }

    public void ToggleSettingsMenu(bool isActive)
    {
        settingsMenu.SetActive(isActive);
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("Play button clicked");
        OnMainMenuExited?.Invoke(fadeOutDelay);
        Invoke(nameof(GoToLobby), fadeOutDelay);
    }

    private void GoToLobby()
    {
        Debug.Log("Transitioning to Lobby...");
        GameManager.Instance.GoToLobby();
    }

    private void OnSettingsButtonClicked()
    {
        // Toggle the settings menu
        ToggleSettingsMenu(true);
        Debug.Log("Settings button clicked");
    }

    private void OnQuitButtonClicked()
    {
        // Quit the application
        Debug.Log("Quit button clicked");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the editor
        #endif
    }
}
