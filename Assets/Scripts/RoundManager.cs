using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void StartRound(int round)
    {
        if (round == 1)
        {
            Debug.Log("RoundManager: Starting exposition round.");
            StartExpositionRound();
        }

        else if (round > 1 && round <= 3)
        {
            Debug.Log($"RoundManager: Starting rising action round {round - 1}.");
            //StartRisingActionRound(round-1);
        }

        else if (round == 4)
        {
            Debug.Log("RoundManager: Starting climax round.");
        }

        else
        {
            Debug.Log("RoundManager: Starting resolution round.");
        }
    }

    public void StartExpositionRound()
    {
        int playerCount = PlayerManager.Instance.GetPlayerCount();
        ExpositionPrompt[] prompts = PromptManager.Instance.GetMultipleRandomPrompts<ExpositionPrompt>(PromptType.Exposition, playerCount);

        if(prompts == null || prompts.Length == 0)
        {
            Debug.Log("RoundManager: Exposition prompts array is null or empty!");
        }
        List<ExpositionPrompt> expositionPrompts = prompts.ToList();
        // assign prompts to players randomly!
        foreach(Player p in PlayerManager.Instance.players)
        {
            int newIndex = Random.Range(0, expositionPrompts.Count());
            SendPromptToPlayer(p, expositionPrompts[newIndex]);
            expositionPrompts.RemoveAt(newIndex);
        }
    }

    public void SendPromptToPlayer(Player player, Prompt prompt)
    {
        var message = new ShowPromptMessage{
            type = "show_prompt",
            text = prompt.promptText,
            inputType = GetInputType(prompt.type)
        };

        ConnectionManager.Instance.SendToPlayer(player, JsonUtility.ToJson(message));
    }

    private string GetInputType(PromptType type)
    {
        switch(type)
        {
            case(PromptType.Exposition):
                return "text";
            case(PromptType.RisingAction):
                return "text";
            case(PromptType.Climax):
                return "choice";
        }

        return null;
    }
}
