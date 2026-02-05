using UnityEngine;

public enum ReactionType
{
    Comedy,
    Triumphant,
    Dark,
    Tragedy,
    Chaos,
    Bittersweet,
    None
}

[System.Serializable]
public class Reaction
{
    public string reactionName;
    public ReactionType reactionType;
    public ReactionType StringToType(string reaction)
    {
        switch(reaction)
        {
            case("comedy"):
                return ReactionType.Comedy;
            case("triumphant"):
                return ReactionType.Triumphant;
            case("dark"):
                return ReactionType.Dark;
            case("tragedy"):
                return ReactionType.Tragedy;
            case("chaos"):
                return ReactionType.Chaos;
            case("bittersweet"):
                return ReactionType.Bittersweet;
        }
        return ReactionType.None;
    }
}
