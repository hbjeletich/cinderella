using UnityEngine;

public class LobbyTester : MonoBehaviour
{
    [Header("Settings")]
    public float exitDelay = 3f;

    private LobbyUI lobbyUI;

    void Start()
    {
        lobbyUI = FindObjectOfType<LobbyUI>();
    }

    private void OnGUI()
    {
        float buttonWidth = 200f;
        float buttonHeight = 40f;
        float padding = 10f;
        float startX = padding;
        float startY = padding;

        if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Enter Lobby"))
        {
            if (lobbyUI != null)
            {
                Debug.Log("LobbyTester: Firing OnLobbyEntered");
                lobbyUI.StartLobby();
            }
            else
            {
                Debug.LogWarning("LobbyTester: No LobbyUI found in scene.");
            }
        }

        if (GUI.Button(new Rect(startX, startY + buttonHeight + padding, buttonWidth, buttonHeight), "Exit Lobby"))
        {
            if (lobbyUI != null)
            {
                Debug.Log($"LobbyTester: Firing OnLobbyExited with delay {exitDelay}");
                lobbyUI.ExitLobby(exitDelay);
            }
            else
            {
                Debug.LogWarning("LobbyTester: No LobbyUI found in scene.");
            }
        }
    }
}