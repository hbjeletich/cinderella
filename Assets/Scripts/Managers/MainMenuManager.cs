using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    
    [Header("Settings Menu")]
    public GameObject settingsMenu;

    public Button settingsCloseButton;

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";

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
        // Load the game scene or start the game
        Debug.Log("Play button clicked");
        GameManager.Instance.SetGameState(GameState.Lobby);
        SceneManager.LoadScene(lobbySceneName);
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
