using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Lobby Settings")]
    public PlayerIcon[] playerIcons;
    private int currentPlayerIconIndex = 0;

    void Start()
    {
        PlayerManager.Instance.OnPlayerCreated += OnPlayerCreated;
    }

    private void OnPlayerCreated(Player player)
    {
        PlayerIcon icon = playerIcons[currentPlayerIconIndex];
        if(icon != null)
        {
            icon.AssignPlayer(player);
            icon.ShowImage();
        }

        currentPlayerIconIndex += 1;

        if(currentPlayerIconIndex >= playerIcons.Length)
        {
            Debug.Log($"UIManager: Player Icon limit reached! Stopping at the last on the list.");
            currentPlayerIconIndex = playerIcons.Length;
        }
    }

}
