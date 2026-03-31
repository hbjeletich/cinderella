using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ReactionData", menuName = "Cinderella/Reaction Data")]
public class ReactionData : ScriptableObject
{
    [System.Serializable]
    public class ReactionEntry
    {
        public ReactionType type;
        public string displayName;
        public Sprite sprite;
    }

    public ReactionEntry[] reactions;

    private Dictionary<ReactionType, ReactionEntry> lookup;

    private void BuildLookup()
    {
        lookup = new Dictionary<ReactionType, ReactionEntry>();
        if (reactions == null) return;
        foreach (var entry in reactions)
            lookup[entry.type] = entry;
    }

    public Sprite GetSprite(ReactionType type)
    {
        if (lookup == null) BuildLookup();
        if (lookup.TryGetValue(type, out var entry))
            return entry.sprite;
        return null;
    }

    public string GetDisplayName(ReactionType type)
    {
        if (lookup == null) BuildLookup();
        if (lookup.TryGetValue(type, out var entry))
            return entry.displayName;
        return type.ToString();
    }
}