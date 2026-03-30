using UnityEngine;
using UnityEngine.InputSystem;

public class BackgroundTestDriver : MonoBehaviour
{
    public BackgroundController backgroundController;
    public PhaseColorConfig colorConfig;

    private int currentPhaseIndex = -1;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame
            && colorConfig.phases.Length > 0
            && !backgroundController.IsTransitioning)
        {
            currentPhaseIndex = (currentPhaseIndex + 1) % colorConfig.phases.Length;
            string phaseName = colorConfig.phases[currentPhaseIndex].phaseName;
            Debug.Log($"Transitioning to: {phaseName}");
            backgroundController.TransitionToPhase(phaseName);
        }
    }
}