using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerIcon : MonoBehaviour
{
    public Player assignedPlayer;
    public Image uiImage;

    public float angle = 15f;
    public float speed = 2f;

    void Start()
    {
        if(uiImage == null)
        {
            uiImage = GetComponent<Image>();
        }

        StartCoroutine(DoRotation());
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

    private IEnumerator DoRotation()
    {
        Quaternion startRot = transform.rotation;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * speed;

            float offset = Mathf.Sin(t) * angle;

            transform.rotation = startRot * Quaternion.Euler(0f, 0f, offset);

            yield return null;
        }

    }
}
