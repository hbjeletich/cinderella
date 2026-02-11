using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerID;
    public string playerName;
    public bool isHost;
    public int score;
    public bool hasSubmittedThisRound = false;
    public Prompt lastPrompt;
    
    public void SetReady(bool ready)
    {
        hasSubmittedThisRound = ready;
    }

    public void SetLastPrompt(Prompt newPrompt)
    {
        lastPrompt = newPrompt;
    }

    public Prompt GetLastPrompt()
    {
        return lastPrompt;
    }
}
