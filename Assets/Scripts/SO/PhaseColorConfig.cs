using UnityEngine;

[CreateAssetMenu(fileName = "PhaseColorConfig", menuName = "Cinderella/Phase Color Config")]
public class PhaseColorConfig : ScriptableObject
{
    [System.Serializable]
    public class TransitionAnimation
    {
        public string animationName;
        public Sprite[] frames;
    }

    [System.Serializable]
    public class PhaseEntry
    {
        public string phaseName;
        [Header("Frame Colors")]
        public Color mainColor = Color.white;
        public Color accentColor = Color.white;
        public Color detailColor = Color.white;
        public Color fineDetailColor = Color.white;

        [Header("Grid (idle before transition)")]
        public Color gridOverlayColor = Color.white;
        public Color gridBackColor = Color.gray;
        public int gridAnimationIndex = 0;
        public float gridDuration = 0.5f;

        [Header("Wipe")]
        public Color transitionColor = Color.white;
        public int wipeAnimationIndex = 1;
        public float wipeDuration = 2f;

        [Header("Grid (idle after transition)")]
        public Color nextGridBackColor = Color.gray;
        public Color nextGridOverlayColor = Color.white;
    }

    [Header("Animations")]
    public TransitionAnimation[] animations;

    [Header("Phase Definitions")]
    public PhaseEntry[] phases;

    public PhaseEntry GetPhase(string phaseName)
    {
        foreach (var entry in phases)
        {
            if (entry.phaseName == phaseName)
                return entry;
        }
        return null;
    }

    public Sprite[] GetFrames(int animIndex)
    {
        if (animations == null || animIndex < 0 || animIndex >= animations.Length) return null;
        return animations[animIndex].frames;
    }
}