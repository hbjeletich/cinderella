using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class LobbyUI : MonoBehaviour
{
    [Header("Player Icons")]
    public List<PlayerIcon> playerIcons = new List<PlayerIcon>();
    private List<PlayerIcon> availableIcons = new List<PlayerIcon>();

    [Header("Player Count Display")]
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI maxPlayersText;
    private int maxPlayers;

    public Action OnLobbyEntered;
    public Action<float> OnLobbyExited;

    void Awake()
    {
        PlayerManager.Instance.OnPlayerCreated += OnPlayerCreated;
        PlayerManager.Instance.OnPlayerDisconnected += OnPlayerDisconnected;
        PlayerManager.Instance.OnPlayerReconnected += OnPlayerReconnected;
        availableIcons = new List<PlayerIcon>(playerIcons);

        maxPlayers = PlayerManager.Instance.maxPlayers;
        UpdatePlayerCount();
    }

    void OnDestroy()
    {
        // unsubscribe so destroyed LobbyUI doesn't leave ghost listeners
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerCreated -= OnPlayerCreated;
            PlayerManager.Instance.OnPlayerDisconnected -= OnPlayerDisconnected;
            PlayerManager.Instance.OnPlayerReconnected -= OnPlayerReconnected;
        }
    }

    public void StartLobby()
    {
        Debug.Log("LobbyUI: Entered Lobby state.");
        OnLobbyEntered?.Invoke();
    }

    void OnPlayerCreated(Player newPlayer)
    {
        AssignPlayerIcon(newPlayer);
        UpdatePlayerCount();
    }

    void OnPlayerDisconnected(Player player)
    {
        UpdatePlayerCount();
    }

    void OnPlayerReconnected(Player player)
    {
        UpdatePlayerCount();
    }

    public void ExitLobby(float delay)
    {   
        Debug.Log($"LobbyUI: Exiting lobby with delay of {delay} seconds.");
        OnLobbyExited?.Invoke(delay);
    }

    public void AssignPlayerIcon(Player player)
    {
        if (playerIcons.Count == 0)
        {
            Debug.LogWarning("LobbyUI: No player icons assigned in the inspector.");
            return;
        }

        // random icon assignment at first
        if (availableIcons.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableIcons.Count);
            PlayerIcon assignedIcon = availableIcons[randomIndex];
            player.playerIcon = assignedIcon;
            Debug.Log($"{player.playerName} assigned to icon {assignedIcon.gameObject.name}");
            assignedIcon.AssignPlayer(player);

            // remove assigned icon from available icons
            List<PlayerIcon> iconList = new List<PlayerIcon>(availableIcons);
            iconList.RemoveAt(randomIndex);
            availableIcons = iconList;
        }
        else
        {
            Debug.LogWarning("LobbyUI: No more available icons to assign.");
        }
    }

    public void SwapPlayerIcon(Player player, PlayerIcon newIcon)
    {
        if (player.playerIcon != null)
        {
            player.playerIcon.ClearAssignment();
        }
        player.playerIcon = newIcon;
        newIcon.AssignPlayer(player);
    }

    /// <summary>
    /// Always derive count from PlayerManager — no independent counter to drift.
    /// </summary>
    public void UpdatePlayerCount()
    {
        int connectedCount = PlayerManager.Instance.GetPlayerCount();

        if (playerCountText != null)
            playerCountText.text = connectedCount.ToString();

        if (maxPlayersText != null)
            maxPlayersText.text = maxPlayers.ToString();
    }
}