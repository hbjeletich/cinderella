using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
    public List<Player> players = new List<Player>();
    public int minPlayers = 2;
    public int maxPlayers = 3;

    public Action<Player> OnPlayerCreated;
    public Action<Player> OnPlayerReady;
    public Action<Player> OnPlayerDisconnected;
    public Action<Player> OnPlayerReconnected;
    
    public static PlayerManager Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {   
            Destroy(gameObject);
        }
    }

    public Player CreatePlayer(string playerID, string deviceId, string playerName)
    {
        GameObject playerObj = new GameObject($"Player_{playerID}");
        playerObj.transform.parent = this.transform;
        Player newPlayer = playerObj.AddComponent<Player>();
        newPlayer.playerName = playerName;
        newPlayer.playerID = playerID;
        newPlayer.deviceId = deviceId;
        newPlayer.isConnected = true;
        //UIManager.Instance.AssignPlayerIcon(newPlayer);
        
        // if first connected player, set as host
        if (GetConnectedPlayers().Count == 0)
        {
            newPlayer.isHost = true;
        }
        
        players.Add(newPlayer);
        Debug.Log($"PlayerManager: Created new player with ID: {playerID}, deviceId: {deviceId}, name: {playerName}. Total players: {GetPlayerCount()}");

        OnPlayerCreated?.Invoke(newPlayer);

        return newPlayer;
    }

    public void DisconnectPlayer(string playerID)
    {
        Player player = GetPlayer(playerID);
        if(player == null) return;

        player.isConnected = false;
        Debug.Log($"PlayerManager: Player {player.playerName} ({playerID}) disconnected.");

        // transfer host if needed
        if(player.isHost)
        {
            player.isHost = false;
            SetNewHost();
        }

        OnPlayerDisconnected?.Invoke(player);
    }

    public Player ReconnectPlayer(Player player, string newConnectionID)
    {
        player.playerID = newConnectionID;
        player.isConnected = true;
        Debug.Log($"PlayerManager: Player {player.playerName} reconnected with new ID: {newConnectionID}");

        // if no current host, this player becomes host
        if(!players.Any(p => p.isHost && p.isConnected))
        {
            player.isHost = true;
            Debug.Log($"PlayerManager: {player.playerName} reassigned as host on reconnect.");
        }

        OnPlayerReconnected?.Invoke(player);
        return player;
    }

    public Player FindByDeviceId(string deviceId)
    {
        if(string.IsNullOrEmpty(deviceId)) return null;
        foreach(Player p in players)
        {
            if(p.deviceId == deviceId)
                return p;
        }
        return null;
    }

    public bool IsNameTaken(string name, Player excludePlayer = null)
    {
        foreach(Player p in players)
        {
            if(p == excludePlayer) continue;
            if(p.playerName != null && p.playerName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public bool IsGameInProgress()
    {
        return GameManager.Instance != null && 
               GameManager.Instance.CurrentState != GameState.Lobby &&
               GameManager.Instance.CurrentState != GameState.Ended;
    }

    public List<Player> GetConnectedPlayers()
    {
        return players.Where(p => p.isConnected).ToList();
    }

    public void ResetForNewGame()
    {
        foreach (Player p in players)
        {
            if (p != null && p.gameObject != null)
                Destroy(p.gameObject);
        }
        players.Clear();
        Debug.Log("PlayerManager: Reset for new game.");
    }

    public void ResetPlayerReady()
    {
        foreach(Player p in players)
        {
            // disconnected players are auto-ready — they can't submit
            p.hasSubmittedThisRound = !p.isConnected;
        }
    }

    public void SetPlayerReady(string playerID)
    {
        Player player = GetPlayer(playerID);
        player.hasSubmittedThisRound = true;
        OnPlayerReady?.Invoke(player);
    }

    public bool ArePlayersReady()
    {
        foreach(Player p in players)
        {
            if(!p.hasSubmittedThisRound) return false;
        }
        return true;
    }

    private void SetNewHost()
    {
        List<Player> connected = GetConnectedPlayers();
        if(connected.Count == 0) return;

        // pick a random connected player
        int randomIndex = UnityEngine.Random.Range(0, connected.Count);
        connected[randomIndex].isHost = true;
        Debug.Log($"PlayerManager: New host assigned: {connected[randomIndex].playerName}");
    }
    
    public Player GetPlayer(string playerID)
    {
        foreach (var player in players)
        {
            if (player.playerID == playerID)
            {
                return player;
            }
        }
        return null;
    }

    public int GetPlayerCount()
    {
        return GetConnectedPlayers().Count;
    }

    public bool ReadyToStart()
    {
        return minPlayers <= GetConnectedPlayers().Count;
    }

    public Player GetHighestScoringPlayer()
    {
        return players.OrderByDescending(p => p.score).First();
    }

    public Player GetLowestScoringPlayer()
    {
        return players.OrderBy(p => p.score).First();
    }

    public List<Player> GetPlayersSortedByScore()
    {
        List<Player> sorted = new List<Player>(players);
        sorted.Sort((a, b) => a.score.CompareTo(b.score));
        return sorted;
    }
}