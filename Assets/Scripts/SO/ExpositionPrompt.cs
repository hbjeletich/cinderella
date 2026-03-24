using UnityEngine;

public class ExpositionPrompt : Prompt
{
    public bool necessity = false;
    public string storyElement;
    public string defaultAnswer;
    // random question options sent to the player's phone
    public string[] questions;
}