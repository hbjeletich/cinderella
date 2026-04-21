using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class PlayerIcon : MonoBehaviour
{
    public Player assignedPlayer;
    public Image uiImage;
    public Sprite playerIcon;
    [Header("Player Name Display")]
    public GameObject displayNameContainer;
    public TextMeshProUGUI playerNameText;

    public float angle = 15f;
    public float speed = 2f;

    void Start()
    {
        if(uiImage == null)
        {
            uiImage = GetComponent<Image>();
        }

        displayNameContainer.SetActive(false);

        StartCoroutine(DoRotation());
    }

    void Update()
    {
        if(assignedPlayer == null) return;
    }

    public void AssignPlayer(Player newPlayer)
    {
        assignedPlayer = newPlayer;

        // on assign, show the image and set the sprite
        if (uiImage != null && playerIcon != null)
        {
            uiImage.sprite = playerIcon;
            ShowImage();

            // store the sprite on the Player too, so the reveal card can display it later
            newPlayer.playerSprite = playerIcon;
        }

        // set player name text
        // limit to 10 characters for now
        if (playerNameText != null)
        {
            displayNameContainer.SetActive(true);
            string displayName = assignedPlayer.playerName;
            if(!string.IsNullOrEmpty(displayName))
            {
                if (displayName.Length > 10)
                {
                    displayName = displayName.Substring(0, 10) + "...";
                }
                playerNameText.text = displayName;
            }
            else
            {
                Debug.Log("PlayerIcon: Player name is null or empty.");
            }
        }
    }

    private void SetAlphaToZero()
    {
        uiImage.DOFade(1f, 0.5f);
    }

    private void SetAlphaToOne()
    {
        uiImage.DOFade(1f, 0.5f);
    }

    public void ClearAssignment()
    {
        assignedPlayer = null;
        HideImage();
    }

    public Player GetPlayer()
    {
        return assignedPlayer;
    }

    public void HideImage()
    {
        SetAlphaToZero();
    }

    public void ShowImage()
    {
        SetAlphaToOne();
    }

    private System.Collections.IEnumerator DoRotation()
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