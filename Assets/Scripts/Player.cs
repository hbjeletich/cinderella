using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerID;
    public string playerName;
    public bool isHost;
    public int score;
    public bool hasSubmittedThisRound = false;
    
    public void SetReady(bool ready)
    {
        hasSubmittedThisRound = ready;
    }
}
