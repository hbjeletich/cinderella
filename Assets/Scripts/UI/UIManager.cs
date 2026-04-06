using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // the goal of this guy is to read game events and assign them properly
    // should be complete middleman

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

    // public void AssignPlayerIcon(Player player)
    // {
    //     lobbyUI?.AssignPlayerIcon(player);
    // }

    public void ExitLobby(float delay)
    {
        Debug.Log($"UIManager: Exiting lobby with delay of {delay} seconds.");
        if(lobbyUI != null)
        {
            lobbyUI.ExitLobby(delay);
        } 
        else
        {
            lobbyUI = FindObjectOfType<LobbyUI>();
            if(lobbyUI != null)
            {
                lobbyUI.ExitLobby(delay);
            }
            else
            {
                Debug.LogWarning("UIManager: LobbyUI not found for ExitLobby");
            }
        }
    }

    public void ShowTimer(int seconds)
    {
        EnsureGameUI();
        gameUI?.ShowTimer(seconds);
    }

    public void UpdateTimer(int seconds)
    {
        gameUI?.UpdateTimer(seconds);
    }

    public void HideTimer()
    {
        gameUI?.HideTimer();
    }

    public void ShowWritingPhase(int roundNumber, float duration)
    {
        EnsureGameUI();
        gameUI?.ShowWritingPhase(roundNumber, duration);
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

    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        if(gameUI != null)
        {
            gameUI.ShowSubmission(player, answer, onComplete, promptText);
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
                gameUI.ShowSubmission(player, answer, onComplete, promptText);
            }
        }
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete, string promptText = null)
    {
        if(gameUI != null)
        {
            gameUI.ShowOptions(player, answers, onComplete, promptText);
        }
        else
        {
            gameUI = FindObjectOfType<GameUI>();
            if(gameUI == null)
            {
                Debug.LogWarning("UIManager: GameUI not found for ShowOptions");
                onComplete?.Invoke();
            } 
            else
            {
                gameUI.ShowOptions(player, answers, onComplete, promptText);
            }
        }
    }

    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        if (gameUI != null)
        {
            gameUI.ShowScoreboard(roundNumber, sortedPlayers, onComplete);
        }
        else
        {
            gameUI = FindObjectOfType<GameUI>();
            if (gameUI == null)
            {
                Debug.LogWarning("UIManager: GameUI not found for ShowScoreboard");
                onComplete?.Invoke(); // in case we cant find it game moves forward!
            }
            else
            {
                gameUI.ShowScoreboard(roundNumber, sortedPlayers, onComplete);
            }
        }
    }

    public void ShowFinalScoreboard(List<Player> sortedPlayers, float holdTime, Action onComplete)
    {
        EnsureGameUI();
        if (gameUI != null)
        {
            gameUI.ShowFinalScoreboard(sortedPlayers, holdTime, onComplete);
        }
        else
        {
            Debug.LogWarning("UIManager: GameUI not found for ShowFinalScoreboard");
            onComplete?.Invoke();
        }
    }

    public void ShowInPhaseNarration(string text, Action onComplete)
    {
        EnsureGameUI();
        if (gameUI != null)
        {
            gameUI.ShowInPhaseNarration(text, onComplete);
        }
        else
        {
            Debug.LogWarning("UIManager: GameUI not found for ShowInPhaseNarration");
            onComplete?.Invoke();
        }
    }

    public void RevealWinnerCard(string winningAnswer, Action onComplete)
    {
        EnsureGameUI();
        if (gameUI != null)
        {
            gameUI.RevealWinnerCard(winningAnswer, onComplete);
        }
        else
        {
            Debug.LogWarning("UIManager: GameUI not found for RevealWinnerCard");
            onComplete?.Invoke();
        }
    }

    public void ShowReactionsAndAuthor(string winningAnswer, Player author,
        Dictionary<Player, Reaction> reactions, Action onComplete)
    {
        EnsureGameUI();
        if (gameUI != null)
        {
            gameUI.ShowReactionsAndAuthor(winningAnswer, author, reactions, onComplete);
        }
        else
        {
            Debug.LogWarning("UIManager: GameUI not found for ShowReactionsAndAuthor");
            onComplete?.Invoke();
        }
    }

    private void OnPlayerCreated(Player player)
    {
        // // todo: switch this to be lobby related!
        // PlayerIcon icon = playerIcons[currentPlayerIconIndex];
        // if(icon != null)
        // {
        //     icon.AssignPlayer(player);
        //     icon.ShowImage();
        // }

        // currentPlayerIconIndex += 1;

        // if(currentPlayerIconIndex >= playerIcons.Length)
        // {
        //     Debug.Log($"UIManager: Player Icon limit reached! Stopping at the last on the list.");
        //     currentPlayerIconIndex = playerIcons.Length;
        // }
    }

    private void EnsureGameUI()
    {
        if(gameUI == null)
            gameUI = FindObjectOfType<GameUI>();
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
        gameUI = null;
        SceneManager.sceneLoaded += OnLobbySceneLoaded;
    }

    private void OnLobbySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name != "Lobby") return;
        SceneManager.sceneLoaded -= OnLobbySceneLoaded;

        lobbyUI = FindObjectOfType<LobbyUI>();
        if(lobbyUI != null)
            lobbyUI.StartLobby();
        else
            Debug.LogError("UIManager: LobbyUI not found after Lobby scene loaded!");
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