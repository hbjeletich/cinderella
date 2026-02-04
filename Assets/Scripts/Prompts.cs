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
            return PromptType.None;
        }
        return PromptType.None;
    }
}

public class ExpositionPrompt : Prompt
{
    public string storyElement;
}

public class RisingActionPrompt : Prompt
{
    public int round;
    public string storyBeat;
    public string[] options;
    public string resonanceTag;
}

public class ClimaxPrompt : Prompt
{
    public string climaxType;
    public string[] protagonistOptions;
    public string[] antagonistOptions;
}

public class ResolutionPrompt : Prompt
{
    public string outcomeCategory;
    public string tone;
}