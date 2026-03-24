using UnityEngine;

public enum ResolutionSegmentType
{
    Exposition,
    Rising1,
    Rising2,
    Rising3,
    Climax,
    Closing,
    None
}

public class ResolutionPrompt : Prompt
{
    public ResolutionSegmentType segmentType;
    public string tone;
    public string climaxOutcome;

    public ResolutionSegmentType StringToSegmentType(string input)
    {
        switch(input.ToLower())
        {
            case "exposition": return ResolutionSegmentType.Exposition;
            case "rising_1": return ResolutionSegmentType.Rising1;
            case "rising_2": return ResolutionSegmentType.Rising2;
            case "rising_3": return ResolutionSegmentType.Rising3;
            case "climax": return ResolutionSegmentType.Climax;
            case "closing": return ResolutionSegmentType.Closing;
        }
        return ResolutionSegmentType.None;
    }
}