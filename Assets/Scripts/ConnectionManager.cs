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

    // SEND MESSAGES
    #region Send Messages

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

    #endregion
    #region Handle Messages

    // HANDLE MESSAGE

    public void HandleWebSocketMessage(string message, string clientID)
    {
        Debug.Log($"ConnectionManager: Received message from {clientID}: {message}");
        // process the message as needed
        var baseMessage = JsonUtility.FromJson<Message>(message);

        switch(baseMessage.type)
        {
            case "join":
                HandleJoinMessage(message, clientID);
                break;
            case "start_game":
                break;
        }
    }

    // MESSAGE HANDLERS

    private void HandleJoinMessage(string rawMessage, string id)
    {
        // parse for player name, create player
        var message = JsonUtility.FromJson<JoinMessage>(rawMessage);
        string playerName = message.playerName;
                
        Player newPlayer = PlayerManager.Instance.GetPlayer(id); 
        if(newPlayer == null)
        {
            Debug.Log($"ConnectionManager: Player with ID {id} does not exist!");
            return;
        }
        else
        {
            newPlayer.playerName = playerName;
        }

        // send JoinedMessage back
        var newMsg = new JoinedMessage {
            type = "joined",
            playerName = playerName,
            isHost = newPlayer.isHost,
            readyToStart = PlayerManager.Instance.ReadyToStart()
        };

        SendMessageToPlayerID(id, JsonUtility.ToJson(newMsg));
    }

    #endregion
}
