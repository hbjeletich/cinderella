using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StoryManager : MonoBehaviour
{
    private ClimaxPrompt chosenClimax;
    private int roundNumber = 0;
    public int RoundNumber => roundNumber;
    public static StoryManager Instance { get; private set; }

    // tone tally across all rounds
    private Dictionary<ReactionType, int> toneTally = new Dictionary<ReactionType, int>();

    // what reaction each answer got
    private List<SubmissionTone> submissionTones = new List<SubmissionTone>();

    // per-rising-action-round tone tracking 
    private Dictionary<int, List<ReactionType>> risingRoundTones = new Dictionary<int, List<ReactionType>>();

    // story variables accumulated across rounds for use in resolution
    // storyElement (exposition), storyBeat (rising action), or climaxType (climax)
    // one day maybe i will come back and make this a cleaner thing but unfortunately. i did not!
    private Dictionary<string, string> storyVariables = new Dictionary<string, string>();

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

    public void RecordStoryVariable(string key, string value)
    {
        // record key and value for dictionary
        storyVariables[key] = value;
        Debug.Log($"StoryManager: Recorded story variable [{key}] = \"{value}\"");
    }

    public string GetStoryVariable(string key)
    {
        storyVariables.TryGetValue(key, out string value);
        return value;
    }

    public Dictionary<string, string> GetAllStoryVariables()
    {
        return new Dictionary<string, string>(storyVariables);
    }

    public string FillPlaceholders(string text)
    {
        foreach (var variable in storyVariables)
            text = text.Replace($"[{variable.Key}]", variable.Value);
        return text;
    }

    public string FillAndCleanTemplate(string template)
    {
        string filled = FillPlaceholders(template);

        // split into sentences, keep only ones with all placeholders filled
        string[] sentences = filled.Split(new string[] { ". " }, System.StringSplitOptions.None);
        List<string> cleanSentences = new List<string>();

        foreach(string sentence in sentences)
        {
            if(string.IsNullOrWhiteSpace(sentence))
                continue;

            if(HasUnfilledPlaceholder(sentence))
            {
                Debug.Log($"StoryManager: Skipping sentence with unfilled placeholder: {sentence}");
                continue;
            }

            cleanSentences.Add(sentence);
        }

        return string.Join(". ", cleanSentences);
    }

    private bool HasUnfilledPlaceholder(string text)
    {
        int start = text.IndexOf('[');
        if(start < 0) return false;
        int end = text.IndexOf(']', start);
        return end > start;
    }

    public List<string> BuildResolutionSegments()
    {
        List<string> segments = new List<string>();

        // exposition segment — no tone
        ResolutionPrompt expSeg = PromptManager.Instance.GetResolutionSegment(ResolutionSegmentType.Exposition);
        if(expSeg != null)
            TryAddSegment(segments, expSeg, "Exposition");

        // rising segments — each uses that round's dominant tone
        ResolutionSegmentType[] risingTypes = new ResolutionSegmentType[]
        {
            ResolutionSegmentType.Rising1,
            ResolutionSegmentType.Rising2,
            ResolutionSegmentType.Rising3
        };

        for(int i = 0; i < risingTypes.Length; i++)
        {
            int risingRound = i + 1;
            ReactionType roundTone = GetDominantRisingTone(risingRound);
            string toneStr = " ";
            if(roundTone != ReactionType.None)
            {
                toneStr = roundTone.ToString().ToLower();
            } else
            {
                Debug.LogWarning($"StoryManager: No dominant tone for rising round {risingRound}");
                toneStr = null;
            }

            ResolutionPrompt risingSeg = PromptManager.Instance.GetResolutionSegment(risingTypes[i], toneStr);
            if(risingSeg != null)
                TryAddSegment(segments, risingSeg, risingTypes[i].ToString());
        }

        // climax segment — no tone
        ResolutionPrompt climaxSeg = PromptManager.Instance.GetResolutionSegment(ResolutionSegmentType.Climax);
        if(climaxSeg != null)
            TryAddSegment(segments, climaxSeg, "Climax");

        // closing segment — climax outcome + overall tone
        string climaxOutcome = GetClimaxOutcome();
        string overallTone = GetDominantTone().ToString().ToLower();
        ResolutionPrompt closingSeg = PromptManager.Instance.GetClosingSegment(overallTone, climaxOutcome);
        if(closingSeg != null)
            TryAddSegment(segments, closingSeg, "Closing");

        Debug.Log($"StoryManager: Built {segments.Count} resolution segments.");
        return segments;
    }

    private void TryAddSegment(List<string> segments, ResolutionPrompt segment, string label)
    {
        string filled = FillAndCleanTemplate(segment.promptText);
        if(!string.IsNullOrWhiteSpace(filled))
        {
            segments.Add(filled);
            Debug.Log($"StoryManager: Added resolution segment [{label}]: {filled}");
        }
    }

    public ClimaxPrompt GetChosenClimax()
    {
        return chosenClimax;
    }

    public void RecordReactions(Dictionary<Player, Reaction> reactions)
    {
        // tally tone
        foreach(var reaction in reactions)
        {
            ReactionType reactionType = reaction.Value.reactionType;
            if(toneTally.ContainsKey(reactionType))
                toneTally[reactionType]++;
            else
                toneTally[reactionType] = 1;
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

    public void RecordRisingRoundTone(int risingRound, ReactionType tone)
    {
        if(!risingRoundTones.ContainsKey(risingRound))
            risingRoundTones[risingRound] = new List<ReactionType>();

        risingRoundTones[risingRound].Add(tone);
        Debug.Log($"StoryManager: Recorded rising round {risingRound} tone: {tone}");
    }

    public ReactionType GetDominantRisingTone(int risingRound)
    {
        if(!risingRoundTones.ContainsKey(risingRound))
            return ReactionType.None;

        List<ReactionType> tones = risingRoundTones[risingRound];

        // tally up
        Dictionary<ReactionType, int> tally = new Dictionary<ReactionType, int>();
        foreach(ReactionType t in tones)
        {
            if(tally.ContainsKey(t))
                tally[t]++;
            else
                tally[t] = 1;
        }

        // find highest
        ReactionType dominant = ReactionType.None;
        int highest = 0;
        foreach(var kv in tally)
        {
            if(kv.Value > highest)
            {
                highest = kv.Value;
                dominant = kv.Key;
            }
        }

        return dominant;
    }

    public string GetClimaxOutcome()
    {
        if(chosenClimax != null && !string.IsNullOrEmpty(chosenClimax.outcomeCategory))
            return chosenClimax.outcomeCategory;
        return null;
    }
}

[System.Serializable]
public struct SubmissionTone
{
    public Player player;
    public string submission;
    public ReactionType majorityReaction;
}