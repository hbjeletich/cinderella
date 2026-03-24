using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

public class RoundManager : MonoBehaviour
{
    private Dictionary<Player, string> submissions = new Dictionary<Player, string>();
    private Dictionary<Player, string> votes = new Dictionary<Player, string>();
    private Dictionary<Player, Reaction> reactions = new Dictionary<Player, Reaction>();

    public event Action OnAllPromptsSubmitted;
    public event Action OnAllReactionsSubmitted;
    public event Action OnAllVotesSubmitted;
    public event Action OnClimaxChoicesReady;
    public static RoundManager Instance { get; private set; }

    // timer settings (exposed to Inspector)
    [Header("Timer Settings")]
    public float promptTimerDuration = 45f;
    public float reactTimerDuration = 15f;
    public float voteTimerDuration = 20f;

    private Coroutine activeTimer;
    private List<string> currentVotingOptions = new List<string>();

    // for climax round use
    private bool isClimaxPicking = false;
    private string protagonistChoice;
    private string antagonistChoice;
    private Player protagonistPlayer;
    private Player antagonistPlayer;

    // for rising action group system
    private List<List<Player>> groups = new List<List<Player>>();
    private List<RisingActionPrompt> groupPrompts = new List<RisingActionPrompt>();
    private int currentGroupIndex = 0;

    public string GetProtagonistChoice() => protagonistChoice;
    public string GetAntagonistChoice() => antagonistChoice;
    public List<List<Player>> GetGroups() => groups;
    public int GetCurrentGroupIndex() => currentGroupIndex;
    public void SetCurrentGroupIndex(int index) => currentGroupIndex = index;
    public RisingActionPrompt GetGroupPrompt(int groupIndex) => groupPrompts[groupIndex];

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

    private void ResetAll()
    {
        Debug.Log("RoundManager: Resetting all round state.");
        StopPhaseTimer();
        PlayerManager.Instance.ResetPlayerReady();

        submissions.Clear();
        reactions.Clear();
        votes.Clear();

        protagonistChoice = null;
        antagonistChoice = null;
        protagonistPlayer = null;
        antagonistPlayer = null;

        groups.Clear();
        groupPrompts.Clear();
        currentGroupIndex = 0;
        currentVotingOptions.Clear();
    }

    #region Timer

    public void StartPhaseTimer(float duration, Action onExpired)
    {
        StopPhaseTimer();
        activeTimer = StartCoroutine(PhaseTimerCoroutine(duration, onExpired));
    }

    public void StopPhaseTimer()
    {
        if(activeTimer != null)
        {
            StopCoroutine(activeTimer);
            activeTimer = null;
        }
        UIManager.Instance?.HideTimer();
    }

    private IEnumerator PhaseTimerCoroutine(float duration, Action onExpired)
    {
        float remaining = duration;
        UIManager.Instance?.ShowTimer(Mathf.CeilToInt(remaining));

        while(remaining > 0)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
            UIManager.Instance?.UpdateTimer(Mathf.CeilToInt(Mathf.Max(0, remaining)));
        }

        activeTimer = null;
        UIManager.Instance?.HideTimer();
        onExpired?.Invoke();
    }

    public void AutoSubmitMissingPrompts()
    {
        Debug.Log("RoundManager: Timer expired — auto-submitting for missing prompt responses.");
        List<Player> missing = new List<Player>();
        foreach(Player p in PlayerManager.Instance.players)
        {
            if(!p.hasSubmittedThisRound)
                missing.Add(p);
        }
        foreach(Player p in missing)
        {
            string fallback = GetDefaultAnswer(p);
            Debug.Log($"RoundManager: Auto-submitting \"{fallback}\" for {p.playerName}");
            HandlePromptSubmission(new SubmitMessage { type = "send_prompt", text = fallback }, p);
        }
    }

    private string GetDefaultAnswer(Player player)
    {
        Prompt prompt = player.GetLastPrompt();
        if(prompt == null) return "...";

        if(prompt is ExpositionPrompt expo)
        {
            if(!string.IsNullOrEmpty(expo.defaultAnswer))
                return expo.defaultAnswer;
            return "...";
        }

        if(prompt is RisingActionPrompt rising)
        {
            if(rising.options != null && rising.options.Length > 0)
                return rising.options[UnityEngine.Random.Range(0, rising.options.Length)];
            return "...";
        }

        return "...";
    }

    public void AutoSubmitMissingReactions()
    {
        Debug.Log("RoundManager: Timer expired — auto-submitting for missing reactions.");
        List<Player> missing = new List<Player>();
        foreach(Player p in PlayerManager.Instance.players)
        {
            if(!p.hasSubmittedThisRound)
                missing.Add(p);
        }
        foreach(Player p in missing)
        {
            HandleReactSubmission(new SubmitMessage { type = "send_react", text = "none" }, p);
        }
    }

    public void AutoSubmitMissingVotes()
    {
        Debug.Log("RoundManager: Timer expired — auto-submitting for missing votes.");
        List<Player> missing = new List<Player>();
        foreach(Player p in PlayerManager.Instance.players)
        {
            if(!p.hasSubmittedThisRound)
                missing.Add(p);
        }
        foreach(Player p in missing)
        {
            if(currentVotingOptions.Count > 0)
            {
                string randomChoice = currentVotingOptions[UnityEngine.Random.Range(0, currentVotingOptions.Count)];
                HandleChoiceSubmission(new SubmitMessage { type = "send_choice", text = randomChoice }, p);
            }
        }
    }

    private void AutoSubmitClimaxPicks()
    {
        Debug.Log("RoundManager: Timer expired — auto-submitting for missing climax picks.");
        ClimaxPrompt climax = StoryManager.Instance.GetChosenClimax();

        if(protagonistChoice == null && protagonistPlayer != null)
            protagonistChoice = climax.protagonistOptions[UnityEngine.Random.Range(0, climax.protagonistOptions.Length)];

        if(antagonistChoice == null && antagonistPlayer != null)
            antagonistChoice = climax.antagonistOptions[UnityEngine.Random.Range(0, climax.antagonistOptions.Length)];

        if(protagonistChoice != null && antagonistChoice != null)
        {
            isClimaxPicking = false;
            OnClimaxChoicesReady?.Invoke();
        }
    }

    #endregion

    #region Disconnect Handling

    public void HandlePlayerDisconnect(Player player)
    {
        if(player.hasSubmittedThisRound)
        {
            Debug.Log($"RoundManager: {player.playerName} disconnected but already submitted this phase.");
            return;
        }

        GameState state = GameManager.Instance.CurrentState;
        Debug.Log($"RoundManager: {player.playerName} disconnected during {state}, auto-submitting.");

        switch(state)
        {
            case GameState.Prompting:
                string fallback = GetDefaultAnswer(player);
                HandlePromptSubmission(new SubmitMessage { type = "send_prompt", text = fallback }, player);
                break;

            case GameState.Reacting:
                HandleReactSubmission(new SubmitMessage { type = "send_react", text = "none" }, player);
                break;

            case GameState.Voting:
                if(isClimaxPicking)
                {
                    // if they were protagonist or antagonist, auto-pick
                    AutoSubmitClimaxPicks();
                }
                else if(currentVotingOptions.Count > 0)
                {
                    string randomChoice = currentVotingOptions[UnityEngine.Random.Range(0, currentVotingOptions.Count)];
                    HandleChoiceSubmission(new SubmitMessage { type = "send_choice", text = randomChoice }, player);
                }
                break;
        }
    }

    #endregion

    public void StartRound(int round)
    {
        // i removed a reset here.... hopefully it does not mess everything up!

        if (round == 1)
        {
            Debug.Log("RoundManager: Starting exposition round.");
            StartExpositionRound();
        }

        else if (round > 1 && round <= 4)
        {
            Debug.Log($"RoundManager: Starting rising action round {round - 1}.");
            StartRisingActionRound(round-1);
        }

        else if (round == 5)
        {
            Debug.Log("RoundManager: Starting climax round.");
            StartClimaxRound();
        }

        else if (round == 6)
        {
            Debug.Log("RoundManager: Starting resolution round.");
            StartResolutionRound();
        }
    }

    public void EndRound()
    {
        Debug.Log("RoundManager: Round over!");
        ResetAll();
    }

    public void StartExpositionRound()
    {
        Debug.Log("RoundManager: Starting exposition round!");

        int playerCount = PlayerManager.Instance.GetPlayerCount();
        List<ExpositionPrompt> expositionPrompts = PromptManager.Instance.GetMultipleRandomPrompts<ExpositionPrompt>(PromptType.Exposition, playerCount);

        if(expositionPrompts == null || expositionPrompts.Count() == 0)
        {
            Debug.Log("RoundManager: Exposition prompts list is null or empty!");
            return;
        }
        // assign prompts to players randomly!
        foreach(Player p in PlayerManager.Instance.players)
        {
            int newIndex = UnityEngine.Random.Range(0, expositionPrompts.Count());
            SendPromptToPlayer(p, expositionPrompts[newIndex]);
            expositionPrompts.RemoveAt(newIndex);
        }

        StartPhaseTimer(promptTimerDuration, AutoSubmitMissingPrompts);
        UIManager.Instance?.ShowWritingPhase(1, promptTimerDuration);
    }

    public void StartRisingActionRound(int round)
    {
        Debug.Log($"RoundManager: Starting rising action round {round}!");

        // create player groups
        groups = CreateGroups(PlayerManager.Instance.players);
        int groupCount = groups.Count;

        Debug.Log($"RoundManager: Created {groupCount} groups for {PlayerManager.Instance.GetPlayerCount()} players.");

        // get round-filtered prompts, one per group
        List<RisingActionPrompt> prompts = PromptManager.Instance.GetRisingActionPromptsByRound(round, groupCount);
        groupPrompts = prompts;

        if(prompts == null || prompts.Count < groupCount)
        {
            Debug.LogError("RoundManager: Not enough rising action prompts for groups!");
            return;
        }

        // send each group their shared prompt — all groups answer simultaneously
        for(int g = 0; g < groupCount; g++)
        {
            RisingActionPrompt prompt = prompts[g];
            foreach(Player p in groups[g])
            {
                SendPromptToPlayer(p, prompt);
            }
            Debug.Log($"RoundManager: Group {g} ({groups[g].Count} players) got prompt: {prompt.promptText}");
        }

        currentGroupIndex = 0;

        StartPhaseTimer(promptTimerDuration, AutoSubmitMissingPrompts);
        UIManager.Instance?.ShowWritingPhase(round + 1, promptTimerDuration);
    }

    private List<List<Player>> CreateGroups(List<Player> allPlayers)
    {
        List<Player> shuffled = new List<Player>(allPlayers);
        for(int i = 0; i < shuffled.Count - 1; i++)
        {
            int r = UnityEngine.Random.Range(i, shuffled.Count);
            Player temp = shuffled[i];
            shuffled[i] = shuffled[r];
            shuffled[r] = temp;
        }

        List<List<Player>> result = new List<List<Player>>();
        int count = shuffled.Count;

        // target groups of 4; allow 3 or 2 for remainders
        List<int> sizes = new List<int>();
        int remaining = count;
        while(remaining > 0)
        {
            if(remaining >= 8)        { sizes.Add(4); remaining -= 4; }
            else if(remaining == 7)   { sizes.Add(4); sizes.Add(3); remaining = 0; }
            else if(remaining == 6)   { sizes.Add(3); sizes.Add(3); remaining = 0; }
            else if(remaining == 5)   { sizes.Add(3); sizes.Add(2); remaining = 0; }
            else if(remaining == 4)   { sizes.Add(4); remaining = 0; }
            else if(remaining == 3)   { sizes.Add(3); remaining = 0; }
            else                      { sizes.Add(remaining); remaining = 0; }
        }

        int index = 0;
        foreach(int size in sizes)
        {
            List<Player> group = new List<Player>();
            for(int i = 0; i < size; i++)
            {
                group.Add(shuffled[index]);
                index++;
            }
            result.Add(group);
        }

        return result;
    }

    public Dictionary<Player, string> GetGroupSubmissions(int groupIndex)
    {
        Dictionary<Player, string> groupSubs = new Dictionary<Player, string>();
        if(groupIndex < 0 || groupIndex >= groups.Count) return groupSubs;

        foreach(Player p in groups[groupIndex])
        {
            if(submissions.ContainsKey(p))
                groupSubs[p] = submissions[p];
        }
        return groupSubs;
    }

    public List<string> BuildVotingOptions(int groupIndex, out HashSet<string> playerAnswers)
    {
        Dictionary<Player, string> groupSubs = GetGroupSubmissions(groupIndex);
        RisingActionPrompt prompt = groupPrompts[groupIndex];

        playerAnswers = new HashSet<string>(groupSubs.Values);
        List<string> options = new List<string>(groupSubs.Values);

        // fill to 4 with decoys from the prompt's options
        int decoyCount = 4 - options.Count;
        if(decoyCount > 0 && prompt.options != null && prompt.options.Length > 0)
        {
            // get decoys that don't match any player answer
            List<string> availableDecoys = new List<string>();
            foreach(string option in prompt.options)
            {
                if(!playerAnswers.Contains(option))
                    availableDecoys.Add(option);
            }

            for(int i = 0; i < decoyCount && availableDecoys.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, availableDecoys.Count);
                options.Add(availableDecoys[idx]);
                availableDecoys.RemoveAt(idx);
            }
        }

        // shuffle all options
        for(int i = 0; i < options.Count - 1; i++)
        {
            int r = UnityEngine.Random.Range(i, options.Count);
            string temp = options[i];
            options[i] = options[r];
            options[r] = temp;
        }

        return options;
    }

    public Player GetAuthorOfAnswer(int groupIndex, string answer)
    {
        Dictionary<Player, string> groupSubs = GetGroupSubmissions(groupIndex);
        foreach(var kvp in groupSubs)
        {
            if(kvp.Value == answer)
                return kvp.Key;
        }
        return null; // decoy
    }

    public void SendGroupVoteToAllPlayers(int groupIndex, List<string> options, string promptText = null)
    {
        PlayerManager.Instance.ResetPlayerReady();
        currentVotingOptions = new List<string>(options);

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswerChoicesMessage{
                type = "show_choices",
                text = string.Join("|", options),
                promptText = promptText,
                myPrompt = false
            };

            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }

        StartPhaseTimer(voteTimerDuration, AutoSubmitMissingVotes);
    }

    public void StartClimaxRound()
    {
        Debug.Log("RoundManager: Starting climax round!");

        isClimaxPicking = true;
        
        ClimaxPrompt climax = StoryManager.Instance.GetChosenClimax();
        
        // send options to the two players
        SendClimaxOptionsToPlayer(protagonistPlayer, climax.protagonistOptions, "protagonist");
        SendClimaxOptionsToPlayer(antagonistPlayer, climax.antagonistOptions, "antagonist");
        
        // build waiting message with scenario context
        string waitingText = DialogueManager.Instance.GetDialogue("climax_waiting");
        waitingText = waitingText.Replace("{protagonist}", protagonistPlayer.playerName);
        waitingText = waitingText.Replace("{antagonist}", antagonistPlayer.playerName);
        waitingText = waitingText.Replace("{climax_prompt}", climax.promptText);

        // send waiting message to everyone else
        foreach(Player p in PlayerManager.Instance.players)
        {
            if(p != protagonistPlayer && p != antagonistPlayer)
            {
                var message = new ShowAnswersMessage{
                    type = "show_answer",
                    text = waitingText,
                    myPrompt = true
                };
                ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
            }
        }

        StartPhaseTimer(promptTimerDuration, AutoSubmitClimaxPicks);
        UIManager.Instance?.ShowWritingPhase(5, promptTimerDuration);
    }

    public void StartResolutionRound()
    {
        Debug.Log("RoundManager: Starting resolution round!");
    }

    public void SendPromptToPlayer(Player player, Prompt prompt, string[] options = null)
    {
        player.SetLastPrompt(prompt);

        // send a random question to the player's phone instead of the TV text
        string textForPlayer = prompt.promptText;
        if(prompt is ExpositionPrompt expo && expo.questions != null && expo.questions.Length > 0)
        {
            textForPlayer = expo.questions[UnityEngine.Random.Range(0, expo.questions.Length)];
        }
        else if(prompt is RisingActionPrompt rising && rising.questions != null && rising.questions.Length > 0)
        {
            textForPlayer = rising.questions[UnityEngine.Random.Range(0, rising.questions.Length)];
        }

        var message = new ShowPromptMessage{
            type = "show_prompt",
            text = textForPlayer,
            inputType = GetInputType(prompt.type)
        };

        ConnectionManager.Instance.SendToPlayer(player, JsonUtility.ToJson(message));
    }

    public void SendOptionsToAllPlayers(Player answeredPlayer, string[] arr)
    {
        PlayerManager.Instance.ResetPlayerReady();
        answeredPlayer.SetReady(true);

        string answers = string.Join("|", arr);

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswersMessage{
                type = "show_choices",
                text = answers,
                myPrompt = (p == answeredPlayer)
            };

            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }
    }

    public void SendReactPromptsToAllPlayers(Player answeredPlayer, string revealText, string promptText = null)
    {
        PlayerManager.Instance.ResetPlayerReady();
        
        foreach(Player p in PlayerManager.Instance.players)
        {
            bool isMyPrompt = (answeredPlayer != null && p == answeredPlayer);
            
            if(isMyPrompt)
                p.SetReady(true);
            
            var message = new ShowAnswersMessage{
                type = "show_answer",
                text = revealText,
                promptText = promptText,
                myPrompt = isMyPrompt
            };
            
            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }

        StartPhaseTimer(reactTimerDuration, AutoSubmitMissingReactions);
    }

    public void SendVotePromptsToAllPlayers(Player answeredPlayer, List<string> options, string promptText = null)
    {
        PlayerManager.Instance.ResetPlayerReady();
        answeredPlayer.SetReady(true);
        currentVotingOptions = new List<string>(options);

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswerChoicesMessage{
                type = "show_choices",
                text = string.Join("|", options),
                promptText = promptText,
                myPrompt = (p == answeredPlayer)
            };

            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }

        StartPhaseTimer(voteTimerDuration, AutoSubmitMissingVotes);
    }

    private void SendClimaxOptionsToPlayer(Player player, string[] options, string role)
    {
        if(player == null)
        {
            Debug.LogError($"RoundManager: Cannot send climax options to null player for role {role}!");
            return;
        }
        
        player.SetLastPrompt(StoryManager.Instance.GetChosenClimax());

        // fill in character names so options read like story actions
        string[] filledOptions = new string[options.Length];
        for(int i = 0; i < options.Length; i++)
        {
            filledOptions[i] = options[i]
                .Replace("[Protagonist]", StoryManager.Instance.GetStoryVariable("protagonist_name"))
                .Replace("[Antagonist]", StoryManager.Instance.GetStoryVariable("antagonist_name"));
        }
        
        ClimaxPrompt climax = StoryManager.Instance.GetChosenClimax();
        var message = new ShowAnswerChoicesMessage{
            type = "show_choices",
            text = string.Join("|", filledOptions),
            promptText = climax?.promptText,
            myPrompt = false
        };
        
        ConnectionManager.Instance.SendToPlayer(player, JsonUtility.ToJson(message));
    }

    public void SendClimaxVoteToAllPlayers(List<string> options, string promptText = null)
    {
        PlayerManager.Instance.ResetPlayerReady();
        currentVotingOptions = new List<string>(options);
        
        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswerChoicesMessage{
                type = "show_choices",
                text = string.Join("|", options),
                promptText = promptText,
                myPrompt = false
            };
            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }

        StartPhaseTimer(voteTimerDuration, AutoSubmitMissingVotes);
    }

    public void HandlePromptSubmission(SubmitMessage message, Player player)
    {
        if(player.hasSubmittedThisRound) return;

        player.SetReady(true);
        submissions[player] = message.text;
        Debug.Log($"RoundManager: Player {player.playerName} submitted {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
            StopPhaseTimer();
            Debug.Log("RoundManager: All players ready!");
            OnAllPromptsSubmitted?.Invoke();
        }
    }

    public void HandleReactSubmission(SubmitMessage message, Player player)
    {
        if(player.hasSubmittedThisRound) return;

        player.SetReady(true);

        Reaction react = new Reaction{
            reactionName = message.text
        };

        react.reactionType = react.StringToType(react.reactionName);
        reactions[player] = react;

        Debug.Log($"RoundManager: Player {player.playerName} reacted with {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
            StopPhaseTimer();
            Debug.Log("RoundManager: All reactions received!");
            OnAllReactionsSubmitted?.Invoke();        
        }
    }

    public void HandleChoiceSubmission(SubmitMessage message, Player player)
    {
        // for climax round
        if(isClimaxPicking)
        {
            if(player == protagonistPlayer)
                protagonistChoice = message.text;
            else if(player == antagonistPlayer)
                antagonistChoice = message.text;
            
            if(protagonistChoice != null && antagonistChoice != null)
            {
                StopPhaseTimer();
                Debug.Log("RoundManager: Both climax choices received!");
                isClimaxPicking = false;
                OnClimaxChoicesReady?.Invoke();
            }
            return;
        }

        if(player.hasSubmittedThisRound) return;

        // for other choices
        player.SetReady(true);
        votes[player] = message.text;
        Debug.Log($"RoundManager: Player {player.playerName} submitted {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
            StopPhaseTimer();
            Debug.Log("RoundManager: All votes received!");
            OnAllVotesSubmitted?.Invoke();
        }
    }

    public List<Player> RollProtagonistAntagonist()
    {
        List<Player> players = PlayerManager.Instance.players;
        if(players.Count < 2)
        {
            Debug.LogError("RoundManager: Not enough players to roll protagonist and antagonist!");
            return null;
        }

        // for now just pick randomly, but could be based on scores or something later
        protagonistPlayer = PlayerManager.Instance.GetHighestScoringPlayer();
        antagonistPlayer = PlayerManager.Instance.GetLowestScoringPlayer();

        Debug.Log($"RoundManager: Rolled protagonist {protagonistPlayer.playerName} and antagonist {antagonistPlayer.playerName}!");

        return new List<Player>{protagonistPlayer, antagonistPlayer};
    }

    private string GetInputType(PromptType type)
    {
        switch(type)
        {
            case(PromptType.Exposition):
                return "text";
            case(PromptType.RisingAction):
                return "text";
            case(PromptType.Climax):
                return "choice";
        }

        return null;
    }

    public string GetWinningChoice()
    {
        Dictionary<string, int> tally = new Dictionary<string, int>();
        
        foreach(string choice in votes.Values)
        {
            if(tally.ContainsKey(choice))
                tally[choice]++;
            else
                tally[choice] = 1;
        }
        
        int maxVotes = tally.Values.Max();
        List<string> winners = tally.Where(kv => kv.Value == maxVotes)
                                    .Select(kv => kv.Key)
                                    .ToList();
        
        if(winners.Count == 1)
            return winners[0];
        
        // tie — pick random
        return winners[UnityEngine.Random.Range(0, winners.Count)];
    }

    public void ClearPerPlayerState()
    {
        votes.Clear();
        reactions.Clear();
        PlayerManager.Instance.ResetPlayerReady();
    }

    public Dictionary<Player, string> GetSubmissions()
    {
        return new Dictionary<Player, string>(submissions);
    }

    public Dictionary<Player, string> GetVotes()
    {
        return new Dictionary<Player, string>(votes);
    }

    public Dictionary<Player, Reaction> GetReactions()
    {
        return new Dictionary<Player, Reaction>(reactions);
    }

    public ReactionType GetMajorityReactionType()
    {
        Dictionary<ReactionType, int> tally = new Dictionary<ReactionType, int>();
        
        foreach(var react in reactions.Values)
        {
            if(react.reactionType == ReactionType.None) continue;

            if(tally.ContainsKey(react.reactionType))
                tally[react.reactionType]++;
            else
                tally[react.reactionType] = 1;
        }

        // everyone timed out
        if(tally.Count == 0)
            return ReactionType.None;
        
        int maxCount = tally.Values.Max();
        List<ReactionType> winners = tally.Where(kv => kv.Value == maxCount)
                                          .Select(kv => kv.Key)
                                          .ToList();
        
        if(winners.Count == 1)
            return winners[0];
        
        // tie — random tiebreak
        return winners[UnityEngine.Random.Range(0, winners.Count)];
    }
}