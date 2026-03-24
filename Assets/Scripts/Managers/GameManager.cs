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

    // resolution title phase
    private bool isResolutionTitlePhase = false;

    [Header("Scoring")]
    public int answerPickedPoints = 500;
    public int votedWithMajorityPoints = 200;
    public int reactionConsensusPoints = 100;
    public int titlePickedPoints = 300;

    [Header("Content Filter")]
    public bool enableProfanityFilter = false;

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
        
        ClimaxPrompt climax = StoryManager.Instance.GetChosenClimax();

        string introText = DialogueManager.Instance.GetDialogue("climax_intro");
        // swap in player names
        introText = introText.Replace("{protagonist}", protagonist.playerName);
        introText = introText.Replace("{antagonist}", antagonist.playerName);
        introText = introText.Replace("{climax_type}", climax.climaxType);
        introText = introText.Replace("{climax_prompt}", climax.promptText);

        UIManager.Instance.ShowNarrative(introText, onComplete: () => {
            SetGameState(GameState.Prompting);
            RoundManager.Instance.StartRound(5);
        });
    }

    private void StartResolutionRound()
    {
        isResolutionTitlePhase = false;

        string introText = DialogueManager.Instance.GetDialogue("resolution_intro");

        UIManager.Instance.ShowNarrative(introText, onComplete: () => {
            StartCoroutine(PlayResolutionSegments());
        });
    }

    private IEnumerator PlayResolutionSegments()
    {
        List<string> segments = StoryManager.Instance.BuildResolutionSegments();

        foreach(string segment in segments)
        {
            bool segmentDone = false;
            UIManager.Instance.ShowNarrative(segment, () => segmentDone = true);
            yield return new WaitUntil(() => segmentDone);
        }

        // small pause before title phase
        yield return new WaitForSeconds(1.5f);

        StartTitlePhase();
    }

    private void StartTitlePhase()
    {
        Debug.Log("GameManager: Starting title phase!");
        isResolutionTitlePhase = true;
        SetGameState(GameState.Prompting);

        // ask every player to write a title
        string titlePromptText = DialogueManager.Instance.GetDialogue("title_prompt");

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowPromptMessage{
                type = "show_prompt",
                text = titlePromptText,
                inputType = "text"
            };
            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }

        RoundManager.Instance.StartPhaseTimer(RoundManager.Instance.promptTimerDuration, () => {
            // auto-submit "Untitled" for players who didn't write a title
            foreach(Player p in PlayerManager.Instance.players)
            {
                if(!p.hasSubmittedThisRound)
                {
                    RoundManager.Instance.HandlePromptSubmission(
                        new SubmitMessage { type = "send_prompt", text = "Untitled" }, p);
                }
            }
        });
        UIManager.Instance.ShowWritingPhase(6, RoundManager.Instance.promptTimerDuration);
    }

    private void HandleTitleSubmissions()
    {
        Debug.Log("GameManager: All titles submitted, starting title vote!");
        currentSubmissions = RoundManager.Instance.GetSubmissions();

        List<string> titles = new List<string>();
        foreach(var sub in currentSubmissions)
        {
            titles.Add(sub.Value);
        }

        RoundManager.Instance.ClearPerPlayerState();
        SetGameState(GameState.Voting);

        // show titles on TV, then send vote to everyone
        UIManager.Instance.ShowOptions(null, titles, onComplete: () => {
            RoundManager.Instance.SendClimaxVoteToAllPlayers(titles);
        });
    }

    private void HandleTitleVoteResult()
    {
        string winningTitle = RoundManager.Instance.GetWinningChoice();
        Debug.Log($"GameManager: Winning title: {winningTitle}");

        // find who wrote it and give them points
        Player author = null;
        foreach(var sub in currentSubmissions)
        {
            if(sub.Value == winningTitle)
            {
                author = sub.Key;
                break;
            }
        }

        if(author != null)
        {
            author.score += titlePickedPoints;
            Debug.Log($"GameManager: {author.playerName} earned {titlePickedPoints} pts (title picked)");
        }

        // voted with majority
        Dictionary<Player, string> votes = RoundManager.Instance.GetVotes();
        foreach(var vote in votes)
        {
            if(vote.Value == winningTitle)
            {
                vote.Key.score += votedWithMajorityPoints;
                Debug.Log($"GameManager: {vote.Key.playerName} earned {votedWithMajorityPoints} pts (voted with majority)");
            }
        }

        // show the winning title on TV
        string authorName = (author != null) ? author.playerName : "someone";
        string revealText = $"\"{winningTitle}\" — titled by {authorName}!";

        SetGameState(GameState.Talking);

        UIManager.Instance.ShowNarrative(revealText, onComplete: () => {
            isResolutionTitlePhase = false;
            EndRound();
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
        // title phase intercept
        if(isResolutionTitlePhase)
        {
            HandleTitleSubmissions();
            return;
        }

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
            
            // record this group's tone for the rising round (game round 2,3,4 → rising 1,2,3)
            int risingRound = currentRound - 1;
            StoryManager.Instance.RecordRisingRoundTone(risingRound, majority);
            
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
        // title vote intercept
        if(isResolutionTitlePhase)
        {
            HandleTitleVoteResult();
            return;
        }

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
            // climax vote — record under both the specific type key and a standard key
            StoryManager.Instance.RecordStoryVariable(StoryManager.Instance.GetChosenClimax().climaxType, winningChoice);
            StoryManager.Instance.RecordStoryVariable("climax_choice", winningChoice);
            StoryManager.Instance.RecordStoryVariable("climax_type", StoryManager.Instance.GetChosenClimax().climaxType);
            StoryManager.Instance.RecordStoryVariable("climax_outcome", StoryManager.Instance.GetChosenClimax().outcomeCategory);

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

        // show the scenario question on TV before revealing options
        ClimaxPrompt climax = StoryManager.Instance.GetChosenClimax();
        string voteIntro = DialogueManager.Instance.GetDialogue("climax_vote_intro");
        voteIntro = voteIntro.Replace("{climax_prompt}", climax.promptText);

        UIManager.Instance.ShowNarrative(voteIntro, onComplete: () => {
            UIManager.Instance.ShowOptions(null, options, onComplete: () => {
                RoundManager.Instance.SendClimaxVoteToAllPlayers(options);
            });
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