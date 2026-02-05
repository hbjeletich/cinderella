using UnityEngine;

public enum PromptType
{
    Exposition,
    RisingAction,
    Climax,
    Resolution,
    None
}

public abstract class Prompt : ScriptableObject
{
    public string id;
    public PromptType type;
    public string promptText;

    public PromptType StringToType(string input)
    {
        switch(input)
        {
            case("EXP"):
                return PromptType.Exposition;
            case("RA"):
                return PromptType.RisingAction;
            case("CLX"):
                return PromptType.Climax;
            case("RES"):
                return PromptType.Resolution;
        }
        return PromptType.None;
    }
}