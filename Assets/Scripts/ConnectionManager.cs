using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
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
        Debug.Log($"ConnectionManager received message from {clientID}: {message}");
        // process the message as needed
    }
}
