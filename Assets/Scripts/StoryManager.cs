using UnityEngine;

public class StoryManager : MonoBehaviour
{
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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGameStateChanged(GameState newState)
    {
        switch(newState)
        {
            case(GameState.Playing):
                RollClimax();
                break;
        }
    }

    private void RollClimax()
    {
        
    }
}
