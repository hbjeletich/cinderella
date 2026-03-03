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

    // rising action group reveal state
    private HashSet<string> currentGroupPlayerAnswers;
    private string currentGroupWinningChoice;

    [Header("Scoring")]
    public int answerPickedPoints = 500;
    public int votedWithMajorityPoints = 200;
    public int reactionConsensusPoints = 100;

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
        ResolutionPrompt resolution = StoryManager.Instance.GetResolutionPrompt();
        string filledText = StoryManager.Instance.FillPlaceholders(resolution.promptText);

        UIManager.Instance.ShowNarrative(introText, onComplete: () => {
            UIManager.Instance.ShowNarrative(filledText, onComplete: () => {
                StoryManager.Instance.OnRoundComplete();
            });
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

        if (currentRound == 1)
        {
            // exposition: record story variables, then show each submission for reactions
            foreach (var submission in currentSubmissions)
            {
                ExpositionPrompt prompt = submission.Key.GetLastPrompt() as ExpositionPrompt;
                if (prompt != null)
                    StoryManager.Instance.RecordStoryVariable(prompt.storyElement, submission.Value);
            }

            shuffledPlayers = currentSubmissions.Keys.ToList();
            ShuffleList(shuffledPlayers);
            currentSubmissionIndex = 0;

            SetGameState(GameState.Reacting);
            ShowNextSubmission();
        }
        else if (currentRound >= 2 && currentRound <= 4)
        {
            // rising action: group-by-group reveal flow
            RoundManager.Instance.SetCurrentGroupIndex(0);
            SetGameState(GameState.Voting);
            ShowNextGroupVoting();
        }
    }

    private void HandleAllReactionsSubmitted()
    {
        int currentRound = StoryManager.Instance.RoundNumber;
        
        // reaction scoring
        ReactionType majority = RoundManager.Instance.GetMajorityReactionType();
        Dictionary<Player, Reaction> reactions = RoundManager.Instance.GetReactions();
        
        foreach(var reaction in reactions)
        {
            if(reaction.Value.reactionType == majority)
            {
                reaction.Key.score += reactionConsensusPoints;
                Debug.Log($"Scoring: {reaction.Key.playerName} earned {reactionConsensusPoints} pts (reaction consensus)");
            }
        }
        
        // tone tracking
        StoryManager.Instance.RecordReactions(reactions);
        
        if(currentRound == 5)
        {
            // climax is done, end round
            EndRound();
            return;
        }
        
        if(currentRound >= 2 && currentRound <= 4)
        {
            // rising action — record tone for the winning answer, then advance to next group
            int groupIdx = RoundManager.Instance.GetCurrentGroupIndex();
            Player author = RoundManager.Instance.GetAuthorOfAnswer(groupIdx, currentGroupWinningChoice);
            StoryManager.Instance.RecordSubmissionTone(author, currentGroupWinningChoice, majority);
            
            // move to next group
            RoundManager.Instance.SetCurrentGroupIndex(groupIdx + 1);
            
            if(RoundManager.Instance.GetCurrentGroupIndex() >= RoundManager.Instance.GetGroups().Count)
            {
                // all groups done
                EndRound();
            }
            else
            {
                // next group's voting
                SetGameState(GameState.Voting);
                ShowNextGroupVoting();
            }
            return;
        }
        
        // exposition — per-submission tone, then advance
        if(currentSubmissionIndex < shuffledPlayers.Count)
        {
            Player reactedTo = shuffledPlayers[currentSubmissionIndex];
            string submission = currentSubmissions[reactedTo];
            StoryManager.Instance.RecordSubmissionTone(reactedTo, submission, majority);
        }
        
        currentSubmissionIndex++;
        ShowNextSubmission();
    }

    private void HandleAllVotesSubmitted()
    {
        Debug.Log("GameManager: All votes submitted, revealing choice");
        
        string winningChoice = RoundManager.Instance.GetWinningChoice();
        int currentRound = StoryManager.Instance.RoundNumber;

        if(currentRound >= 2 && currentRound <= 4)
        {
            // rising action group vote result
            int groupIdx = RoundManager.Instance.GetCurrentGroupIndex();
            RisingActionPrompt prompt = RoundManager.Instance.GetGroupPrompt(groupIdx);
            currentGroupWinningChoice = winningChoice;
            
            // record the story variable
            if(prompt != null)
                StoryManager.Instance.RecordStoryVariable(prompt.storyBeat, winningChoice);
            
            // score: author of winning answer gets points (if it was a player, not a decoy)
            Player author = RoundManager.Instance.GetAuthorOfAnswer(groupIdx, winningChoice);
            if(author != null)
            {
                author.score += answerPickedPoints;
                Debug.Log($"GameManager: {author.playerName} earned {answerPickedPoints} pts (answer picked)");
            }
            
            // voted with majority
            Dictionary<Player, string> votes = RoundManager.Instance.GetVotes();
            foreach(var vote in votes)
            {
                if(vote.Value == winningChoice)
                {
                    vote.Key.score += votedWithMajorityPoints;
                    Debug.Log($"GameManager: {vote.Key.playerName} earned {votedWithMajorityPoints} pts (voted with majority)");
                }
            }
            
            // show winning answer on TV, then reveal author, then react
            SetGameState(GameState.Reacting);
            
            string authorName = (author != null) ? author.playerName : "the narrator";
            string revealText = $"\"{winningChoice}\" — written by {authorName}!";
            
            UIManager.Instance.ShowSubmission(author, winningChoice, onComplete: () => {
                // now show author reveal as narrative, then send react prompts
                UIManager.Instance.ShowNarrative($"This was {authorName}'s answer!", onComplete: () => {
                    RoundManager.Instance.SendReactPromptsToAllPlayers(null, winningChoice);
                });
            }, promptText: prompt?.promptText);
            
            return;
        }

        if(currentRound == 5)
        {
            // climax vote
            StoryManager.Instance.RecordStoryVariable(StoryManager.Instance.GetChosenClimax().climaxType, winningChoice);

            // score
            foreach(var submission in currentSubmissions)
            {
                if(submission.Value == winningChoice)
                {
                    submission.Key.score += answerPickedPoints;
                    Debug.Log($"GameManager: {submission.Key.playerName} earned {answerPickedPoints} pts (answer picked)");
                    break;
                }
            }
            
            Dictionary<Player, string> votes = RoundManager.Instance.GetVotes();
            foreach(var vote in votes)
            {
                if(vote.Value == winningChoice)
                {
                    vote.Key.score += votedWithMajorityPoints;
                    Debug.Log($"GameManager: {vote.Key.playerName} earned {votedWithMajorityPoints} pts (voted with majority)");
                }
            }

            string climaxText = StoryManager.Instance.GetChosenClimax().promptText;
            
            SetGameState(GameState.Reacting);
            
            UIManager.Instance.ShowSubmission(null, winningChoice, onComplete: () => {
                RoundManager.Instance.SendReactPromptsToAllPlayers(null, winningChoice);
            }, promptText: climaxText);
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

    /// <summary>
    /// Rising action group-by-group reveal: show the group's prompt on TV, then send vote options.
    /// </summary>
    private void ShowNextGroupVoting()
    {
        int groupIdx = RoundManager.Instance.GetCurrentGroupIndex();
        var groups = RoundManager.Instance.GetGroups();

        if(groupIdx >= groups.Count)
        {
            EndRound();
            return;
        }

        RoundManager.Instance.ClearPerPlayerState();

        RisingActionPrompt prompt = RoundManager.Instance.GetGroupPrompt(groupIdx);
        HashSet<string> playerAnswers;
        List<string> options = RoundManager.Instance.BuildVotingOptions(groupIdx, out playerAnswers);
        currentGroupPlayerAnswers = playerAnswers;

        Debug.Log($"GameManager: Showing Group {groupIdx} voting — prompt: {prompt.promptText}, {options.Count} options");

        // show the prompt on TV first, then send vote choices to all players
        UIManager.Instance.ShowNarrative(prompt.promptText, onComplete: () => {
            UIManager.Instance.ShowOptions(null, options, onComplete: () => {
                RoundManager.Instance.SendGroupVoteToAllPlayers(groupIdx, options);
            });
        });
    }

    private void EndRound()
    {
        Debug.Log("GameManager: Round complete!");
        StartCoroutine(EndRoundCoroutine());
    }

    private IEnumerator EndRoundCoroutine()
    {
        int currentRound = StoryManager.Instance.RoundNumber;
        List<Player> sorted = PlayerManager.Instance.GetPlayersSortedByScore();

        bool scoreboardDone = false;
        UIManager.Instance.ShowScoreboard(currentRound, sorted, () => scoreboardDone = true);
        yield return new WaitUntil(() => scoreboardDone);

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