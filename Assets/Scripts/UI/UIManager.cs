using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class UIManager : MonoBehaviour
{
    // the goal of this guy is to read game events and assign them properly
    // should be complete middleman
    [Header("Lobby Settings")]
    public PlayerIcon[] playerIcons;
    private int currentPlayerIconIndex = 0;

    private LobbyUI lobbyUI;
    private GameUI gameUI;

    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        PlayerManager.Instance.OnPlayerCreated += OnPlayerCreated;
        GameManager.Instance.OnSceneChanged += OnSceneChanged;
    }

    public void ShowNarrative(string text, Action onComplete)
    {
        if(gameUI != null)
        {
            gameUI.ShowNarrative(text, onComplete);
        }
        else
        {
            Debug.LogWarning("UIManager: GameUI not found for ShowNarrative");
            onComplete?.Invoke(); // in case we cant find it game moves forward!
        }
    }

    public void ShowSubmission(Player player, string answer, Action onComplete)
    {
        if(gameUI != null)
        {
            gameUI.ShowSubmission(player, answer, onComplete);
        }
        else
        {
            gameUI = FindObjectOfType<GameUI>();
            if(gameUI == null)
            {
                Debug.LogWarning("UIManager: GameUI not found for ShowSubmission");
                onComplete?.Invoke(); // in case we cant find it game moves forward!
            } 
            else
            {
                gameUI.ShowSubmission(player, answer, onComplete);
            }
        }
    }

    private void OnPlayerCreated(Player player)
    {
        // todo: switch this to be lobby related!
        PlayerIcon icon = playerIcons[currentPlayerIconIndex];
        if(icon != null)
        {
            icon.AssignPlayer(player);
            icon.ShowImage();
        }

        currentPlayerIconIndex += 1;

        if(currentPlayerIconIndex >= playerIcons.Length)
        {
            Debug.Log($"UIManager: Player Icon limit reached! Stopping at the last on the list.");
            currentPlayerIconIndex = playerIcons.Length;
        }
    }

    private void OnSceneChanged(string sceneName)
    {
        switch(sceneName)
        {
            case("Game"):
                InitGameScene();
                break;
            case("Lobby"):
                InitLobbyScene();
                break;
        }
    }

    private void InitGameScene()
    {
        // wait a few seconds 
        StartCoroutine(InitGameSceneCoroutine());
    }

    private void InitLobbyScene()
    {
        lobbyUI = FindObjectOfType<LobbyUI>();
        gameUI = null;
    }

    private IEnumerator InitGameSceneCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        lobbyUI = null;
        gameUI = FindObjectOfType<GameUI>();
        
        if (gameUI == null)
        {
            Debug.LogError("UIManager: Could not find GameUI!");
        }
    }

}
