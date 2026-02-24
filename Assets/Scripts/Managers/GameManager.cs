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
        RoundManager.Instance.OnAllVotesSubmitted += HandleAllVotesSubmitted;
        RoundManager.Instance.OnClimaxChoicesReady += HandleClimaxChoicesReady;
    }

    private void OnDestroy()
    {
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnAllPromptsSubmitted -= HandleAllPromptsSubmitted;
            RoundManager.Instance.OnAllReactionsSubmitted -= HandleAllReactionsSubmitted;
            RoundManager.Instance.OnAllVotesSubmitted -= HandleAllVotesSubmitted;
            RoundManager.Instance.OnClimaxChoicesReady -= HandleClimaxChoicesReady;
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
        List<Player> protagonistAntagonist = RoundManager.Instance.RollProtagonistAntagonist();
        // if i find i need to reference these a few times, i will refashion this
        Player protagonist = protagonistAntagonist[0];
        Player antagonist = protagonistAntagonist[1];

        Debug.Log($"GameManager: Protagonist is {protagonist.playerName}, Antagonist is {antagonist.playerName}");
        
        string introText = DialogueManager.Instance.GetDialogue("climax_intro");
        // swap in player names
        introText = introText.Replace("{protagonist}", protagonist.playerName);
        introText = introText.Replace("{antagonist}", antagonist.playerName);

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
        int currentRound = StoryManager.Instance.RoundNumber;
        
        if(currentRound == 5)
        {
            // climax is done, end round
            EndRound();
            return;
        }
        
        currentSubmissionIndex++;
        
        if(currentRound == 1)
            ShowNextSubmission();
        else
            ShowNextVoting();
    }

    private void HandleAllVotesSubmitted()
    {
        Debug.Log("GameManager: All votes submitted, revealing choice");
        
        string winningChoice = RoundManager.Instance.GetWinningChoice();
        int currentRound = StoryManager.Instance.RoundNumber;
        
        if(currentRound == 5)
        {
            // climax — no single "current player", show with climax context
            string climaxText = StoryManager.Instance.GetChosenClimax().promptText;
            
            SetGameState(GameState.Reacting);
            
            UIManager.Instance.ShowSubmission(null, winningChoice, onComplete: () => {
                RoundManager.Instance.SendReactPromptsToAllPlayers(null, winningChoice);
            }, promptText: climaxText);
        }
        else
        {
            // rising action — show as normal submission from the player, then react as normal
            Player currentPlayer = shuffledPlayers[currentSubmissionIndex];
            string promptText = currentPlayer.GetLastPrompt()?.promptText;
            
            SetGameState(GameState.Reacting);
            
            UIManager.Instance.ShowSubmission(currentPlayer, winningChoice, onComplete: () => {
                RoundManager.Instance.SendReactPromptsToAllPlayers(currentPlayer, winningChoice);
            }, promptText: promptText);
        }
    }

    private void HandleClimaxChoicesReady()
    {
        Debug.Log("GameManager: Both climax choices in, sending vote to all");
        
        string protagonistChoice = RoundManager.Instance.GetProtagonistChoice();
        string antagonistChoice = RoundManager.Instance.GetAntagonistChoice();
        
        List<string> options = new List<string>{ protagonistChoice, antagonistChoice };
        ShuffleList(options);
        
        SetGameState(GameState.Voting);
        
        UIManager.Instance.ShowOptions(null, options, onComplete: () => {
            RoundManager.Instance.SendClimaxVoteToAllPlayers(options);
        });
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
        string promptText = currentPlayer.GetLastPrompt()?.promptText;
        
        Debug.Log($"GameManager: Showing submission from {currentPlayer.playerName}");

        UIManager.Instance.ShowSubmission(currentPlayer, submission, onComplete: () => {
            RoundManager.Instance.SendReactPromptsToAllPlayers(currentPlayer, submission);
        }, promptText: promptText);
    }

    private void ShowNextVoting()
    {
        RoundManager.Instance.ClearPerPlayerState();

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
