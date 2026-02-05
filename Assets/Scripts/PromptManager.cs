using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PromptManager : MonoBehaviour
{

    [Header("Prompt Arrays")]
    public ExpositionPrompt[] expositionPrompts;
    public RisingActionPrompt[] risingActionPrompts;
    public ClimaxPrompt[] climaxPrompts;
    public ResolutionPrompt[] resolutionPrompts;

    public static PromptManager Instance { get; private set; }

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

    public Prompt GetRandomPrompt(PromptType type)
    {
        switch(type)
        {
            case PromptType.Exposition:
                return expositionPrompts[Random.Range(0, expositionPrompts.Length)];

            case PromptType.RisingAction:
                return risingActionPrompts[Random.Range(0, risingActionPrompts.Length)];

            case PromptType.Climax:
                return climaxPrompts[Random.Range(0, climaxPrompts.Length)];

            case PromptType.Resolution:
                return resolutionPrompts[Random.Range(0, resolutionPrompts.Length)];
        }

        return null;
    }

    public Prompt[] GetMultipleRandomPrompts(PromptType type, int num)
    {
        switch(type)
        {
            case PromptType.Exposition:
                return GetMultipleRandomPromptsFromArray(num, expositionPrompts);

            case PromptType.RisingAction:
                return GetMultipleRandomPromptsFromArray(num, risingActionPrompts);

            case PromptType.Climax:
                return GetMultipleRandomPromptsFromArray(num, climaxPrompts);

            case PromptType.Resolution:
                return GetMultipleRandomPromptsFromArray(num, resolutionPrompts);
        }

        return null;
    }

    private Prompt[] GetMultipleRandomPromptsFromArray(int x, Prompt[] prompts)
    {
        // cast to list because they're easier to modify
        List<Prompt> promptList = prompts.ToList();
        List<Prompt> selectedPrompts = new List<Prompt>();

        Debug.Log($"PromptManager: Need {x} prompts in array");

        x = Mathf.Clamp(x, 0, promptList.Count());

        if(x == promptList.Count())
        {
            selectedPrompts = promptList;
        } else
        {
            while(x > 0)
            {
                // add random prompt to array
                int newIndex = Random.Range(0, promptList.Count());
                selectedPrompts.Add(promptList[newIndex]);
                promptList.RemoveAt(newIndex);
            }
        }

        Debug.Log($"PromptManager: Returning {selectedPrompts.Count()} items in array.");
        return selectedPrompts.ToArray();
    }

    // public RisingActionPrompt GetRisingActionByRound(int round)
    // {
    //     return null;
    // }

    // under construction!
    // public Prompt FindPromptByID(string id)
    // {
    //     if(id.Contains("EXP"))
    //     {
    //         return SearchPromptListByID(id, expositionPrompts);
    //     }

    //     return null;
    // }

    // private Prompt SearchPromptListByID(string id, Prompt[] prompts)
    // {
    //     foreach(Prompt prompt in prompts)
    //     {
    //         if(prompt.id == id)
    //         {
    //             return prompt;
    //         }
    //     }

    //     return null;
    // }

#if UNITY_EDITOR
   [MenuItem("Tools/Load Prompts")]
    public static void LoadPrompts()
    {
        PromptManager manager = FindObjectOfType<PromptManager>();
        if(manager == null)
        {
            Debug.LogError("Cannot find prompt manager!");
        }
        
        manager.expositionPrompts = Resources.LoadAll<ExpositionPrompt>("Scriptables/Prompts/EXP");
        manager.risingActionPrompts = Resources.LoadAll<RisingActionPrompt>("Scriptables/Prompts/RA");
        manager.climaxPrompts = Resources.LoadAll<ClimaxPrompt>("Scriptables/Prompts/CLX");
        manager.resolutionPrompts = Resources.LoadAll<ResolutionPrompt>("Scriptables/Prompts/RES");
    }
#endif
}
