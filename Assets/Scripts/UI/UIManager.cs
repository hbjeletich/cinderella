using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // the goal of this guy is to read game events and assign them properly
    // should be complete middleman
    [Header("Lobby Settings")]
    public PlayerIcon[] playerIcons;
    private int currentPlayerIconIndex = 0;

    [Header("Canvas Prefabs")]
    private Canvas lobbyCanvas;

    public static UIManager Instance { get; private set; }

    void Awake()
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
        PlayerManager.Instance.OnPlayerCreated += OnPlayerCreated;
        GameManager.Instance.OnSceneChanged += OnSceneChanged;
    }

    private void OnPlayerCreated(Player player)
    {
        // todo: switch this to be lobby related!
        PlayerIcon icon = playerIcons[currentPlayerIconIndex];
        if(icon != null)
        {
            icon.AssignPlayer(player);
            icon.ShowImage();
        }

        currentPlayerIconIndex += 1;

        if(currentPlayerIconIndex >= playerIcons.Length)
        {
            Debug.Log($"UIManager: Player Icon limit reached! Stopping at the last on the list.");
            currentPlayerIconIndex = playerIcons.Length;
        }
    }

    private void OnSceneChanged(string sceneName)
    {
        switch(sceneName)
        {
            case("Game"):
                InitGameScene();
                break;
        }
    }

    private void InitGameScene()
    {
        // Find game UI manager
    }

}
