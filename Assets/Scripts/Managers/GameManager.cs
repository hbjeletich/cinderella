using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public enum GameState
{
    Lobby, // in lobby
    Talking, // players should just be listening
    Prompting, // players submitting prompts
    Reacting, // players should be reacting
    Voting,
    
    Ended
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState, GameState> OnGameStateChanged;
    public event Action<string> OnSceneChanged;

    private GameState currentState;
    private GameState lastState;

    private Dictionary<Player, string> currentSubmissions;
    private List<Player> shuffledPlayers;
    private int currentSubmissionIndex = 0;

    public GameState CurrentState => currentState;

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

        RoundManager.Instance.OnAllPromptsSubmitted += HandleAllPromptsSubmitted;
        RoundManager.Instance.OnAllReactionsSubmitted += HandleAllReactionsSubmitted;
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnAllPromptsSubmitted -= HandleAllPromptsSubmitted;
            RoundManager.Instance.OnAllReactionsSubmitted -= HandleAllReactionsSubmitted;
        }
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

    public void StartRound(int roundNumber)
    {
        Debug.Log($"GameManager: StartRound called for round {roundNumber}");
        StartCoroutine(StartRoundCoroutine(roundNumber));
    }

    private IEnumerator StartRoundCoroutine(int roundNumber)
    {
        yield return new WaitForSeconds(1f);

        SetGameState(GameState.Talking);
        
        if(roundNumber == 1)
        {
            StartExpositionRound();
        }
        else if (roundNumber > 1 && roundNumber <= 4)
        {
            StartRisingActionRound(roundNumber);
        }
        else if (roundNumber == 5)
        {
            StartClimaxRound();
        }
        else
        {
            StartResolutionRound();
        }
    }

    private void StartExpositionRound()
    {
        string introText = DialogueManager.Instance.GetDialogue("exposition_intro");

        UIManager.Instance.ShowNarrative(introText, onComplete: () =>
        {
            SetGameState(GameState.Prompting);
            RoundManager.Instance.StartRound(1);
        });
    }

    private void StartRisingActionRound(int roundNumber)
    {
        string key = $"round_{roundNumber}_intro";
        string introText = DialogueManager.Instance.GetDialogue(key);

        UIManager.Instance.ShowNarrative(introText, onComplete: () =>
        {
            SetGameState(GameState.Prompting);
            RoundManager.Instance.StartRound(roundNumber);
        });
    }

    private void StartClimaxRound()
    {
        string introText = DialogueManager.Instance.GetDialogue("climax_intro");
        
        UIManager.Instance.ShowNarrative(introText, onComplete: () => {
            SetGameState(GameState.Prompting);
            RoundManager.Instance.StartRound(5);
        });
    }

    private void StartResolutionRound()
    {
        string introText = DialogueManager.Instance.GetDialogue("resolution_intro");
        
        UIManager.Instance.ShowNarrative(introText, onComplete: () => {
            SetGameState(GameState.Prompting);
            RoundManager.Instance.StartRound(6);
        });
    }

    public void HandlePromptSubmission(SubmitMessage message, string id)
    {
        Player player = PlayerManager.Instance.GetPlayer(id);
        RoundManager.Instance.HandlePromptSubmission(message, player);
    }

    public void HandleReactSubmission(SubmitMessage message, string id)
    {
        Player player = PlayerManager.Instance.GetPlayer(id);
        RoundManager.Instance.HandleReactSubmission(message, player);
    }

    public void HandleChoiceSubmission(SubmitMessage message, string id)
    {
        Player player = PlayerManager.Instance.GetPlayer(id);
        RoundManager.Instance.HandleChoiceSubmission(message, player);
    }

    private void HandleAllPromptsSubmitted()
    {
        Debug.Log("GameManager: All prompts submitted, starting reaction phase");
        
        currentSubmissions = RoundManager.Instance.GetSubmissions();
        int currentRound = StoryManager.Instance.RoundNumber;
        
        shuffledPlayers = currentSubmissions.Keys.ToList();
        ShuffleList(shuffledPlayers);
        
        currentSubmissionIndex = 0;
        
        if(currentRound == 1)
        {
            SetGameState(GameState.Reacting);
            ShowNextSubmission();
        }
        else
        {
            SetGameState(GameState.Voting);
            ShowNextVoting();
        }
    }

    private void HandleAllReactionsSubmitted()
    {
        Debug.Log("GameManager: All reactions submitted for current answer");
        
        currentSubmissionIndex++;
        
        ShowNextSubmission();
    }

    private void ShowNextSubmission()
    {
        if (currentSubmissionIndex >= shuffledPlayers.Count)
        {
            EndRound();
            return;
        }
        
        Player currentPlayer = shuffledPlayers[currentSubmissionIndex];
        string submission = currentSubmissions[currentPlayer];
        
        Debug.Log($"GameManager: Showing submission from {currentPlayer.playerName}");
        
        UIManager.Instance.ShowSubmission(currentPlayer, submission, onComplete: () => {
            RoundManager.Instance.SendReactPromptsToAllPlayers(currentPlayer);
        });
    }

    private void ShowNextVoting()
    {
        if (currentSubmissionIndex >= shuffledPlayers.Count)
        {
            EndRound();
            return;
        }
        
        Player currentPlayer = shuffledPlayers[currentSubmissionIndex];
        string submission = currentSubmissions[currentPlayer];
        RisingActionPrompt prompt = currentPlayer.GetLastPrompt() as RisingActionPrompt;
        if(prompt != null)
        {
            Debug.Log($"GameManager: Got last Rising Action Prompt from {currentPlayer.playerName}");
            List<string> options = prompt.options.ToList();
            options.Add(submission);
            ShuffleList<string>(options);

            Debug.Log($"GameManager: Showing submission from {currentPlayer.playerName}");
        
            UIManager.Instance.ShowOptions(currentPlayer, options, onComplete: () => {
                RoundManager.Instance.SendVotePromptsToAllPlayers(currentPlayer, options);
            });
        }
    }

    private void EndRound()
    {
        Debug.Log("GameManager: Round complete!");
        RoundManager.Instance.EndRound();
        StoryManager.Instance.OnRoundComplete();
    }

    private void ShuffleList<T>(List<T> list)
    {
        int count = list.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int r = UnityEngine.Random.Range(i, count);
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }
}
