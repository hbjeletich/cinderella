using UnityEngine;

[System.Serializable]
public class Message
{
    public string type;
}

[System.Serializable]
public class JoinMessage
{
    // Client to Server : I have joined!
    public string type = "join";
    public string playerName;
}

[System.Serializable]
public class JoinedMessage
{
    // Server to Client : Here is Who You Are
    public string type = "joined";
    public string playerName;
    public bool isHost;
    public bool readyToStart;
}

[System.Serializable]
public class StartMessage 
{
    // Client to Server : Start the game!
    public string type = "start_game";
}

[System.Serializable]
public class ShowPromptMessage
{
    // Server to Client : Here is your prompt!
    public string type = "show_prompt";
    public string text;
    public string inputType;
}

