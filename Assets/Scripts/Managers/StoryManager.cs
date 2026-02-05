using UnityEngine;

public class StoryManager : MonoBehaviour
{
    private ClimaxPrompt chosenClimax;
    private int roundNumber = 0;
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
        switch(lastState)
        {
            case(GameState.Lobby):
                RollClimax();
                roundNumber = 1;
                break;
            case(lastState.Talking):
                StartRound();
        }
    }

    private void RollClimax()
    {
        chosenClimax = PromptManager.Instance.GetRandomPrompt<ClimaxPrompt>(PromptType.Climax);
    }

    public void StartRound(int round)
    {
        // when a round starts, alert the round manager to start a new round and give it the number
        RoundManager.Instance.StartRound(round);
    }
}
