using UnityEngine;

public class BackgroundMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public float startingSpeed = 0.1f;
    public float acceleration = 0.1f;

    [Header("References")]
    public Renderer backgroundRenderer;
    private float currentSpeed;
    private bool isAccelerating = false;
    private Material bgMaterial;
    void Start()
    {
        // subscribe to OnLobbyEntered event to trigger entrance animation
        LobbyUI lobbyUI = FindObjectOfType<LobbyUI>();

        if (lobbyUI != null)
        {
            lobbyUI.OnLobbyEntered += StartMovement;
            lobbyUI.OnLobbyExited += ExitLobby;
        }

        if (backgroundRenderer != null)
        {
            bgMaterial = backgroundRenderer.material;
        }
    }

    void Update()
    {
        if(isAccelerating)
        {
            currentSpeed += acceleration * Time.deltaTime;
            bgMaterial.SetFloat("_rotation_speed", currentSpeed);
        }
    }

    public void StartMovement()
    {
        currentSpeed = startingSpeed;
        bgMaterial.SetFloat("_rotation_speed", currentSpeed);
        bgMaterial.SetFloat("_sin_toggle", 1f);
    }

    public void ExitLobby(float delay)
    {
        bgMaterial.SetFloat("_sin_toggle", 0f);
        isAccelerating = true;
    }


}
