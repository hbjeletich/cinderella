using UnityEngine;
using System.Collections.Generic;

public class StoryManager : MonoBehaviour
{
    private ClimaxPrompt chosenClimax;
    private int roundNumber = 0;
    public int RoundNumber => roundNumber;
    public static StoryManager Instance { get; private set; }

    // global tone tally across all rounds
    private Dictionary<ReactionType, int> toneTally = new Dictionary<ReactionType, int>();
    
    // what reaction each answer got
    private List<SubmissionTone> submissionTones = new List<SubmissionTone>();

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

    void Start()
    {
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    public void OnGameStateChanged(GameState newState, GameState lastState)
    {
        if (lastState == GameState.Lobby && newState == GameState.Talking)
        {
            RollClimax();
            roundNumber = 1;
            StartRound(roundNumber);
        }
    }

    private void RollClimax()
    {
        chosenClimax = PromptManager.Instance.GetRandomPrompt<ClimaxPrompt>(PromptType.Climax);
        Debug.Log($"StoryManager: Climax chosen: {chosenClimax.promptText}");
    }

    public void StartRound(int round)
    {
        // when a round starts, alert the game manager to start a new round and give it the number
        GameManager.Instance.StartRound(round);
    }

    public void OnRoundComplete()
    {
        roundNumber++;

        if(roundNumber > 6)
        {
            EndGame();
        }
        else
        {
            StartRound(roundNumber);
        }
    }

    private void EndGame()
    {
        Debug.Log("StoryManager: Game complete!");
        GameManager.Instance.SetGameState(GameState.Ended);

        string endText = DialogueManager.Instance.GetDialogue("game_over");

        // probs move this to game manager?
        // UIManager.Instance.ShowNarrative(endText, onComplete: () =>
        // {
        //     // show scores, return to lobby? i'll do this later
        // });
    }

    public ClimaxPrompt GetChosenClimax()
    {
        return chosenClimax;
    }

    public void RecordReactions(Dictionary<Player, Reaction> reactions)
    {
        // tally global tone
        foreach(var kvp in reactions)
        {
            ReactionType rt = kvp.Value.reactionType;
            if(toneTally.ContainsKey(rt))
                toneTally[rt]++;
            else
                toneTally[rt] = 1;
        }
        
        Debug.Log($"StoryManager: Tone tally updated. {toneTally.Count} distinct tones tracked.");
    }

    public void RecordSubmissionTone(Player player, string submission, ReactionType majorityReaction)
    {
        submissionTones.Add(new SubmissionTone{
            player = player,
            submission = submission,
            majorityReaction = majorityReaction
        });
        
        Debug.Log($"StoryManager: Recorded tone for {player.playerName}'s submission: {majorityReaction}");
    }

    public Dictionary<ReactionType, int> GetToneTally()
    {
        return toneTally;
    }

    public List<SubmissionTone> GetSubmissionTones()
    {
        return submissionTones;
    }

    public ReactionType GetDominantTone()
    {
        ReactionType dominant = ReactionType.None;
        int highest = 0;
        
        foreach(var tally in toneTally)
        {
            if(tally.Value > highest)
            {
                highest = tally.Value;
                dominant = tally.Key;
            }
        }
        
        return dominant;
    }
}

[System.Serializable]
public struct SubmissionTone
{
    public Player player;
    public string submission;
    public ReactionType majorityReaction;
}