using UnityEngine;
using System.Linq;
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
    }

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
    }

    private List<List<Player>> CreateGroups(List<Player> allPlayers)
    {
        List<Player> shuffled = new List<Player>(allPlayers);
        // shuffle
        for(int i = 0; i < shuffled.Count - 1; i++)
        {
            int r = UnityEngine.Random.Range(i, shuffled.Count);
            Player temp = shuffled[i];
            shuffled[i] = shuffled[r];
            shuffled[r] = temp;
        }

        List<List<Player>> result = new List<List<Player>>();
        int count = shuffled.Count;

        // determine group sizes based on player count
        List<int> sizes = new List<int>();
        int remaining = count;
        while(remaining > 0 && sizes.Count < 3)
        {
            if(remaining >= 5)
            {
                // if over or equal 5, get rid of 3 of them
                sizes.Add(3);
                remaining -= 3;
            }
            else if(remaining == 4)
            {
                // if 4 left, make two groups of 2
                sizes.Add(2);
                sizes.Add(2);
                remaining = 0;
            }
            else
            {
                // add the rest
                sizes.Add(remaining);
                remaining = 0;
            }
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

    public void SendGroupVoteToAllPlayers(int groupIndex, List<string> options)
    {
        PlayerManager.Instance.ResetPlayerReady();

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswerChoicesMessage{
                type = "show_choices",
                text = string.Join("|", options),
                myPrompt = false // everyone votes, no exclusions
            };

            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }
    }

    public void StartClimaxRound()
    {
        Debug.Log("RoundManager: Starting climax round!");

        isClimaxPicking = true;
        
        ClimaxPrompt climax = StoryManager.Instance.GetChosenClimax();
        
        // send options to the two players
        SendClimaxOptionsToPlayer(protagonistPlayer, climax.protagonistOptions, "protagonist");
        SendClimaxOptionsToPlayer(antagonistPlayer, climax.antagonistOptions, "antagonist");
        
        // send waiting message to everyone else
        foreach(Player p in PlayerManager.Instance.players)
        {
            if(p != protagonistPlayer && p != antagonistPlayer)
            {
                var message = new ShowAnswersMessage{
                    type = "show_answer",
                    text = "The heroes and villains are choosing their fate...",
                    myPrompt = true
                };
                ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
            }
        }
    }

    public void StartResolutionRound()
    {
        Debug.Log("RoundManager: Starting resolution round!");
    }

    public void SendPromptToPlayer(Player player, Prompt prompt, string[] options = null)
    {
        player.SetLastPrompt(prompt);

        var message = new ShowPromptMessage{
            type = "show_prompt",
            text = prompt.promptText,
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

    public void SendReactPromptsToAllPlayers(Player answeredPlayer, string revealText)
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
                myPrompt = isMyPrompt
            };
            
            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }
    }

    public void SendVotePromptsToAllPlayers(Player answeredPlayer, List<string> options)
    {
        PlayerManager.Instance.ResetPlayerReady();
        answeredPlayer.SetReady(true);

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswerChoicesMessage{
                type = "show_choices",
                text = string.Join("|", options),
                myPrompt = (p == answeredPlayer)
            };

            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }
    }

    private void SendClimaxOptionsToPlayer(Player player, string[] options, string role)
    {
        if(player == null)
        {
            Debug.LogError($"RoundManager: Cannot send climax options to null player for role {role}!");
            return;
        }
        
        player.SetLastPrompt(StoryManager.Instance.GetChosenClimax());
        
        var message = new ShowAnswerChoicesMessage{
            type = "show_choices",
            text = string.Join("|", options),
            myPrompt = false
        };
        
        ConnectionManager.Instance.SendToPlayer(player, JsonUtility.ToJson(message));
    }

    public void SendClimaxVoteToAllPlayers(List<string> options)
    {
        PlayerManager.Instance.ResetPlayerReady();
        
        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswerChoicesMessage{
                type = "show_choices",
                text = string.Join("|", options),
                myPrompt = false
            };
            ConnectionManager.Instance.SendToPlayer(p, JsonUtility.ToJson(message));
        }
    }

    public void HandlePromptSubmission(SubmitMessage message, Player player)
    {
        player.SetReady(true);
        submissions.Add(player, message.text);
        Debug.Log($"RoundManager: Player {player.playerName} submitted {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
            Debug.Log("RoundManager: All players ready!");

            OnAllPromptsSubmitted?.Invoke();
        }
    }

    public void HandleReactSubmission(SubmitMessage message, Player player)
    {
        player.SetReady(true);

        Reaction react = new Reaction{
            reactionName = message.text
        };

        react.reactionType = react.StringToType(react.reactionName);
        reactions[player] = react;

        Debug.Log($"RoundManager: Player {player.playerName} reacted with {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
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
                Debug.Log("RoundManager: Both climax choices received!");
                isClimaxPicking = false;
                OnClimaxChoicesReady?.Invoke();
            }
            return;
        }

        // for other choices
        player.SetReady(true);
        votes.Add(player, message.text);
        Debug.Log($"RoundManager: Player {player.playerName} submitted {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
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

        // collect all choices that tied for the most votes
        List<string> winners = new List<string>();
        foreach(var choice in tally)
        {
            if(choice.Value == maxVotes)
                winners.Add(choice.Key);
        }
        
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
            if(tally.ContainsKey(react.reactionType))
                tally[react.reactionType]++;
            else
                tally[react.reactionType] = 1;
        }
        
        int maxCount = tally.Values.Max();
        List<ReactionType> winners = new List<ReactionType>();
        foreach(var reaction in tally)
        {
            if(reaction.Value == maxCount)
                winners.Add(reaction.Key);
        }
        
        if(winners.Count == 1)
            return winners[0];
        
        // tie — random tiebreak
        return winners[UnityEngine.Random.Range(0, winners.Count)];
    }
}