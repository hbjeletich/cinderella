using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PromptManager : MonoBehaviour
{
    [Header("Prompt Lists")]
    // originally arrays because I <3 arrays but i need to change these too much to be arrays!
    public List<ExpositionPrompt> expositionPrompts = new List<ExpositionPrompt>();
    public List<RisingActionPrompt> risingActionPrompts = new List<RisingActionPrompt>();
    public List<ClimaxPrompt> climaxPrompts = new List<ClimaxPrompt>();
    public List<ResolutionPrompt> resolutionPrompts = new List<ResolutionPrompt>();

    public static PromptManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPrompts();
    }

    public T GetRandomPrompt<T>(PromptType type) where T : Prompt
    {
        List<T> pool = GetPromptList<T>(type);
        if (pool.Count() == 0)
            return null;
        return pool[Random.Range(0, pool.Count())];
    }

    public List<T> GetMultipleRandomPrompts<T>(PromptType type, int count) where T : Prompt
    {
        switch (type)
        {
            case PromptType.Exposition:
                return GetExpositionPrompts(
                    count,
                    expositionPrompts) as List<T>;
            case PromptType.RisingAction:
                return GetRandomFromList(count, risingActionPrompts) as List<T>;
            case PromptType.Climax:
                return GetRandomFromList(count, climaxPrompts) as List<T>;
            case PromptType.Resolution:
                return GetRandomFromList(count, resolutionPrompts) as List<T>;
        }

        Debug.LogError("PromptManager: Unknown PromptType!");
        return new List<T>();
    }

    private static List<T> GetRandomFromList<T>(int count, List<T> source) where T : Prompt
    {
        if (source == null || source.Count() == 0 || count <= 0)
            return new List<T>();

        List<T> pool = source;
        List<T> result = new List<T>();

        count = Mathf.Clamp(count, 0, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private static List<ExpositionPrompt> GetExpositionPrompts(int count, List<ExpositionPrompt> source)
    {
        if (source == null || source.Count() == 0)
            return new List<ExpositionPrompt>();

        List<ExpositionPrompt> necessary = source.Where(p => p.necessity).ToList();
        List<ExpositionPrompt> optional = source.Where(p => !p.necessity).ToList();

        int remaining = Mathf.Max(0, count - necessary.Count);

        if(remaining > 0)
        {
            List<ExpositionPrompt> randomOptional = GetRandomFromList<ExpositionPrompt>(remaining, optional);
            necessary.AddRange(randomOptional);
        }

        return necessary;
    }

    private List<T> GetPromptList<T>(PromptType type) where T : Prompt
    {
        switch (type)
        {
            case PromptType.Exposition:
                return expositionPrompts as List<T>;

            case PromptType.RisingAction:
                return risingActionPrompts as List<T>;

            case PromptType.Climax:
                return climaxPrompts as List<T>;

            case PromptType.Resolution:
                return resolutionPrompts as List<T>;

            default:
                return new List<T>();
        }
    }

    public void LoadPrompts()
    {
        expositionPrompts.AddRange(Resources.LoadAll<ExpositionPrompt>("Scriptables/Prompts/EXP"));
        risingActionPrompts.AddRange(Resources.LoadAll<RisingActionPrompt>("Scriptables/Prompts/RA"));
        climaxPrompts.AddRange(Resources.LoadAll<ClimaxPrompt>("Scriptables/Prompts/CLX"));
        resolutionPrompts.AddRange(Resources.LoadAll<ResolutionPrompt>("Scriptables/Prompts/RES"));

        Debug.Log("PromptManager: Prompts loaded successfully.");
    }
}
