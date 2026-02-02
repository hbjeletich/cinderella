using UnityEngine;
using Fleck;
using System.Collections.Generic;

public class ConnectionManager : MonoBehaviour
{
    public Dictionary<string, IWebSocketConnection> Connections => Server.Connections;
    // public Player hostPlayer { get; private set; }

    // instance
    public static ConnectionManager Instance;
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

    public void HandleWebSocketMessage(string message, string clientID)
    {
        Debug.Log($"ConnectionManager: Received message from {clientID}: {message}");
        // process the message as needed
        // for now, set name
        Player player = PlayerManager.Instance.GetPlayer(clientID);
        if (player != null)
        {
            player.playerName = message;
        }
    }

    public void SentToAll(string message)
    {
        foreach (var connection in Connections.Values)
        {
            connection.Send(message);
        }
        Debug.Log($"ConnectionManager: Sent message to all clients: {message}");
    }

    public void SendToPlayer(Player player, string message)
    {
        SendMessageToPlayerID(player.playerID, message);
    }

    private void SendMessageToPlayerID(string playerID, string message)
    {
        if (Connections.TryGetValue(playerID, out var connection))
        {
            connection.Send(message);
            Debug.Log($"ConnectionManager: Sent message to {playerID}: {message}");
        }
        else
        {
            Debug.LogWarning($"ConnectionManager: No connection found for player ID: {playerID}");
        }
    }
}
