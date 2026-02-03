using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    public Player assignedPlayer;
    public Image uiImage;

    void Start()
    {
        if(uiImage == null)
        {
            uiImage = GetComponent<Image>();
        }
    }

    void Update()
    {
        if(assignedPlayer == null) return;
    }

    public void AssignPlayer(Player newPlayer)
    {
        assignedPlayer = newPlayer;
    }

    public Player GetPlayer()
    {
        return assignedPlayer;
    }

    public void HideImage()
    {
        uiImage.color = Color.black;
    }

    public void ShowImage()
    {
        uiImage.color = Color.white;
    }
}
