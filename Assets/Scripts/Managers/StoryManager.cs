using UnityEngine;

public class StoryManager : MonoBehaviour
{
    private ClimaxPrompt chosenClimax;
    private int roundNumber = 0;
    public int RoundNumber => roundNumber;
    public static StoryManager Instance { get; private set; }

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
}
