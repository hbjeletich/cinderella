using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public enum GameState
{
    Lobby, // in lobby
    Talking, // players should just be listening
    Prompting, // players submitting prompts
    Reacting, // players should be reacting
    // voting?
    
    Ended
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState, GameState> OnGameStateChanged;
    public event Action<string> OnSceneChanged;

    private GameState currentState;
    private GameState lastState;

    private void Awake()
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

    private void Start()
    {
        currentState = GameState.Lobby;
    }

    public void SetGameState(GameState newState)
    {
        lastState = currentState;
        currentState = newState;
        OnGameStateChanged?.Invoke(newState, lastState);
    }

    public void StartGame()
    {
        SetGameState(GameState.Talking);
        ChangeScene("Game");
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        OnSceneChanged?.Invoke(sceneName);
    }
}
