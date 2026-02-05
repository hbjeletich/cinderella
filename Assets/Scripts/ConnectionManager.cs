using UnityEngine;
using Fleck;
using System.Collections.Generic;

public class ConnectionManager : MonoBehaviour
{
    public Dictionary<string, IWebSocketConnection> Connections => Server.Connections;
    

    private bool readyToStart = false;

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
                HandleStartMessage();
                break;
            case "send_prompt":
                HandleSubmitPromptMessage(message, clientID);
                break;
            case "send_react":
                HandleSubmitReactionMessage(message, clientID);
                break;
        }
    }

    // MESSAGE HANDLERS

    private void HandleJoinMessage(string rawMessage, string id)
    {
        Player newPlayer = PlayerManager.Instance.CreatePlayer(id);
        // parse for player name, create player
        var message = JsonUtility.FromJson<JoinMessage>(rawMessage);
        string playerName = message.text;
        newPlayer.playerName = playerName;

        // if we are ready to start and we weren't before, send to all. else, send to just one
        bool nowReady = PlayerManager.Instance.ReadyToStart();
        if(nowReady == true && readyToStart == false)
        {
            readyToStart = true;
            // update everyone
            foreach(Player p in PlayerManager.Instance.players)
            {
                if(p == newPlayer)
                {
                    var newMsg = new JoinedMessage {
                        type = "joined",
                        playerName = playerName,
                        isHost = newPlayer.isHost,
                        readyToStart = nowReady
                    };

                    SendMessageToPlayerID(id, JsonUtility.ToJson(newMsg));
                }
                else
                {
                    var newMsg = new JoinedMessage {
                        type = "joined",
                        playerName = p.playerName,
                        isHost = p.isHost,
                        readyToStart = nowReady
                    };

                    SendMessageToPlayerID(p.playerID, JsonUtility.ToJson(newMsg));
                }
            }
        } else
        {
            // send JoinedMessage back just to the one player
            var newMsg = new JoinedMessage {
                type = "joined",
                playerName = playerName,
                isHost = newPlayer.isHost,
                readyToStart = PlayerManager.Instance.ReadyToStart()
            };

            SendMessageToPlayerID(id, JsonUtility.ToJson(newMsg));
        }
    }

    private void HandleStartMessage()
    {
        var message = new Message{
            type = "start_game"
        };
        SentToAll(JsonUtility.ToJson(message));
        GameManager.Instance.StartGame();
    }

    private void HandleSubmitPromptMessage(string rawMessage, string id)
    {
        var message = JsonUtility.FromJson<SubmitMessage>(rawMessage);
        RoundManager.Instance.HandlePromptSubmission(message, id);
    }

    private void HandleSubmitReactionMessage(string rawMessage, string id)
    {
        var message = JsonUtility.FromJson<SubmitMessage>(rawMessage);
        RoundManager.Instance.HandleReactSubmission(message, id);
    }

    #endregion
}
