using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

public class RoundManager : MonoBehaviour
{
    private Dictionary<Player, string> submissions = new Dictionary<Player, string>();
    private Dictionary<Player, string> votes = new Dictionary<Player, string>();
    private List<Reaction> reactions = new List<Reaction>();

    public event Action OnAllPromptsSubmitted;
    public event Action OnAllReactionsSubmitted;
    public event Action OnAllVotesSubmitted;
    public static RoundManager Instance { get; private set; }

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
        PlayerManager.Instance.ResetPlayerReady();

        submissions.Clear();
        reactions.Clear();
        votes.Clear();
    }

    public void StartRound(int round)
    {
        ResetAll();

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
        // rising action
        Debug.Log("RoundManager: Starting rising action round!");

        int playerCount = PlayerManager.Instance.GetPlayerCount();
        List<RisingActionPrompt> risingActionPrompts = PromptManager.Instance.GetMultipleRandomPrompts<RisingActionPrompt>(PromptType.RisingAction, playerCount);

        if(risingActionPrompts == null || risingActionPrompts.Count() == 0)
        {
            Debug.Log("RoundManager: Rising Action prompts list is null or empty!");
            return;
        }

        // assign prompts to players randomly!
        foreach(Player p in PlayerManager.Instance.players)
        {
            int newIndex = UnityEngine.Random.Range(0, risingActionPrompts.Count());
            SendPromptToPlayer(p, risingActionPrompts[newIndex]);
            risingActionPrompts.RemoveAt(newIndex);
        }
    }

    public void StartClimaxRound()
    {
        Debug.Log("RoundManager: Starting climax round!");
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
        answeredPlayer.SetReady(true);

        foreach(Player p in PlayerManager.Instance.players)
        {
            var message = new ShowAnswersMessage{
                type = "show_answer",
                text = revealText,
                myPrompt = (p == answeredPlayer)
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
        reactions.Add(react);

        Debug.Log($"RoundManager: Player {player.playerName} reacted with {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
            Debug.Log("RoundManager: All reactions received!");
            OnAllReactionsSubmitted?.Invoke();        
        }
    }

    public void HandleChoiceSubmission(SubmitMessage message, Player player)
    {
        player.SetReady(true);
        votes.Add(player, message.text);
        Debug.Log($"RoundManager: Player {player.playerName} submitted {message.text}");

        if(PlayerManager.Instance.ArePlayersReady())
        {
            Debug.Log("RoundManager: All votes received!");
            OnAllVotesSubmitted?.Invoke();
        }
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
}
