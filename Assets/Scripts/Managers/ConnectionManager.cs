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

    public void ResetForNewGame()
    {
        readyToStart = false;
        Debug.Log("ConnectionManager: Reset for new game.");
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
        if(!player.isConnected)
        {
            Debug.Log($"ConnectionManager: Skipping send to disconnected player {player.playerName}");
            return;
        }
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

    private void SendError(string connectionID, string errorText)
    {
        var msg = new ErrorMessage { text = errorText };
        SendMessageToPlayerID(connectionID, JsonUtility.ToJson(msg));
    }

    #endregion
    #region Handle Messages

    // HANDLE MESSAGE

    public void HandleWebSocketMessage(string message, string clientID)
    {
        Debug.Log($"ConnectionManager: Received message from {clientID}: {message}");
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
            case "send_choice":
                HandleSubmitChoiceMessage(message, clientID);
                break;
        }
    }

    // MESSAGE HANDLERS

    private void HandleJoinMessage(string rawMessage, string id)
    {
        var message = JsonUtility.FromJson<JoinMessage>(rawMessage);
        string playerName = FilterText(message.text);
        // limit name length to prevent UI issues
        if (playerName.Length > 12)
            playerName = playerName.Substring(0, 12);
        string deviceId = message.deviceId;

        // --- RECONNECT CHECK ---
        Player existing = PlayerManager.Instance.FindByDeviceId(deviceId);
        if(existing != null)
        {
            HandleReconnect(existing, id);
            return;
        }

        // --- NEW PLAYER CHECKS ---

        // reject if game already in progress
        if(PlayerManager.Instance.IsGameInProgress())
        {
            Debug.Log($"ConnectionManager: Rejecting join from {playerName} — game in progress.");
            SendError(id, "Game is already in progress!");
            return;
        }

        // reject if name is taken
        if(PlayerManager.Instance.IsNameTaken(playerName))
        {
            Debug.Log($"ConnectionManager: Rejecting join — name '{playerName}' already taken.");
            SendError(id, $"The name \"{playerName}\" is already taken!");
            return;
        }

        // reject if at max players
        if(PlayerManager.Instance.GetPlayerCount() >= PlayerManager.Instance.maxPlayers)
        {
            Debug.Log($"ConnectionManager: Rejecting join — lobby full.");
            SendError(id, "Lobby is full!");
            return;
        }

        // --- CREATE NEW PLAYER ---
        Player newPlayer = PlayerManager.Instance.CreatePlayer(id, deviceId, playerName);
        //newPlayer.playerName = playerName;

        // lobby ready-to-start logic
        bool nowReady = PlayerManager.Instance.ReadyToStart();
        if(nowReady && !readyToStart)
        {
            readyToStart = true;
            // update everyone
            foreach(Player p in PlayerManager.Instance.players)
            {
                var newMsg = new JoinedMessage {
                    type = "joined",
                    playerName = p.playerName,
                    isHost = p.isHost,
                    readyToStart = true
                };
                SendToPlayer(p, JsonUtility.ToJson(newMsg));
            }
        }
        else
        {
            // send JoinedMessage back just to the new player
            var newMsg = new JoinedMessage {
                type = "joined",
                playerName = playerName,
                isHost = newPlayer.isHost,
                readyToStart = PlayerManager.Instance.ReadyToStart()
            };
            SendMessageToPlayerID(id, JsonUtility.ToJson(newMsg));
        }
    }

    private void HandleReconnect(Player player, string newConnectionID)
    {
        Debug.Log($"ConnectionManager: Reconnecting player {player.playerName} (old: {player.playerID}, new: {newConnectionID})");

        PlayerManager.Instance.ReconnectPlayer(player, newConnectionID);

        var msg = new RejoinedMessage {
            playerName = player.playerName
        };
        SendMessageToPlayerID(newConnectionID, JsonUtility.ToJson(msg));
    }

    // DISCONNECT

    public void HandlePlayerDisconnect(string connectionID)
    {
        Player player = PlayerManager.Instance.GetPlayer(connectionID);
        if (player == null) return;

        PlayerManager.Instance.DisconnectPlayer(connectionID);

        if (PlayerManager.Instance.IsGameInProgress())
        {
            RoundManager.Instance.HandlePlayerDisconnect(player);
            
            // if all players disconnected mid-game, return to lobby
            if (PlayerManager.Instance.GetConnectedPlayers().Count == 0)
            {
                Debug.Log("ConnectionManager: All players disconnected — returning to lobby.");
                GameManager.Instance.ReturnToLobby();
            }
        }
    }

    // OTHER HANDLERS

    private string FilterText(string text)
    {
        if(GameManager.Instance.enableProfanityFilter && ProfanityFilter.Instance != null)
            return ProfanityFilter.Instance.Censor(text);
        return text;
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
        message.text = FilterText(message.text);
        GameManager.Instance.HandlePromptSubmission(message,id);
    }

    private void HandleSubmitReactionMessage(string rawMessage, string id)
    {
        var message = JsonUtility.FromJson<SubmitMessage>(rawMessage);
        GameManager.Instance.HandleReactSubmission(message,id);
    }

    private void HandleSubmitChoiceMessage(string rawMessage, string id)
    {
        var message = JsonUtility.FromJson<SubmitMessage>(rawMessage);
        GameManager.Instance.HandleChoiceSubmission(message,id);
    }

    #endregion
}