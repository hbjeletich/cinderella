using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public List<Player> players = new List<Player>();
    public int minPlayers = 2;
    public int maxPlayers = 3;
    
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

    public Player CreatePlayer(string playerID)
    {
        GameObject playerObj = new GameObject($"Player_{playerID}");
        playerObj.transform.parent = this.transform;
        Player newPlayer = playerObj.AddComponent<Player>();
        newPlayer.playerID = playerID;
        
        // if first player, set as host
        if (players.Count == 0)
        {
            newPlayer.isHost = true;
        }
        
        players.Add(newPlayer);
        Debug.Log($"PlayerManager: Created new player with ID: {playerID}");

        return newPlayer;
    }

    public void RemovePlayer(string playerID)
    {
        Player playerToRemove = null;
        foreach (var player in players)
        {
            if (player.playerID == playerID)
            {
                playerToRemove = player;
                break;
            }
        }
        
        if (playerToRemove != null)
        {
            if (playerToRemove.isHost)
            {
                SetNewHost(playerToRemove);
            }
            
            players.Remove(playerToRemove);
            Destroy(playerToRemove.gameObject);

            Debug.Log($"PlayerManager: Removed player with ID: {playerID}");
        }
    }

    private void SetNewHost(Player removedPlayer)
    {
        if (players.Count <= 1)
        {
            return;
        }
        
        // pick a random player that isn't the one being removed
        List<Player> eligiblePlayers = new List<Player>();
        foreach (var player in players)
        {
            if (player.playerID != removedPlayer.playerID)
            {
                eligiblePlayers.Add(player);
            }
        }
        
        if (eligiblePlayers.Count > 0)
        {
            int randomIndex = Random.Range(0, eligiblePlayers.Count);
            eligiblePlayers[randomIndex].isHost = true;
            Debug.Log($"PlayerManager: New host assigned with ID: {eligiblePlayers[randomIndex].playerID}");
        }
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

    public bool ReadyToStart()
    {
        return minPlayers <= players.Count;
    }
}