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
    public string text;
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

[System.Serializable]
public class SubmitMessage
{
    // Client to Server : Here is my response!
    public string type;
    public string text;
}

[System.Serializable]
public class PromptsSubmittedMessage
{
    // Server to Client : All prompts submitted!
    public string type = "prompts_submitted";
}

[System.Serializable]
public class ShowAnswersMessage
{
    // Server to Client : React to Prompt!
    public string type = "show_answer";
    public string text;
    public bool myPrompt = false;
}

