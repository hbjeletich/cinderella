using UnityEngine;
using System;

public enum GameState
{
    Lobby,
    Playing,
    Ended
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    private GameState currentState;

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
        CurrentState = GameState.Lobby;
    }

    public GameState CurrentState
    {
        get { return currentState; }
        private set
        {
            if (currentState != value)
            {
                currentState = value;
                OnGameStateChanged?.Invoke(currentState);
            }
        }
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
    }

    public void StartGame()
    {
        SetGameState(GameState.Playing);
    }
}
