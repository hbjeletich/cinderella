using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPrompts();
    }

    public T GetRandomPrompt<T>(PromptType type) where T : Prompt
    {
        T[] pool = GetPromptArray<T>(type);
        if (pool.Length == 0)
            return null;
        return pool[Random.Range(0, pool.Length)];
    }

    public T[] GetMultipleRandomPrompts<T>(PromptType type, int count) where T : Prompt
    {
        switch (type)
        {
            case PromptType.Exposition:
                return GetExpositionPrompts(
                    count,
                    expositionPrompts) as T[];
            case PromptType.RisingAction:
                return GetRandomFromArray(count, risingActionPrompts) as T[];
            case PromptType.Climax:
                return GetRandomFromArray(count, climaxPrompts) as T[];
            case PromptType.Resolution:
                return GetRandomFromArray(count, resolutionPrompts) as T[];
        }

        Debug.LogError("PromptManager: Unknown PromptType!");
        return System.Array.Empty<T>();
    }

    private static T[] GetRandomFromArray<T>(int count, T[] source) where T : Prompt
    {
        if (source == null || source.Length == 0 || count <= 0)
            return System.Array.Empty<T>();

        List<T> pool = source.ToList();
        List<T> result = new List<T>();

        count = Mathf.Clamp(count, 0, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result.ToArray();
    }

    private static ExpositionPrompt[] GetExpositionPrompts(int count, ExpositionPrompt[] source)
    {
        if (source == null || source.Length == 0)
            return System.Array.Empty<ExpositionPrompt>();

        List<ExpositionPrompt> necessary = source.Where(p => p.necessity).ToList();
        List<ExpositionPrompt> optional = source.Where(p => !p.necessity).ToList();

        int remaining = Mathf.Max(0, count - necessary.Count);

        ExpositionPrompt[] randomOptional = GetRandomFromArray(remaining, optional.ToArray());

        return necessary.Concat(randomOptional).Take(count).ToArray();
    }

    private T[] GetPromptArray<T>(PromptType type) where T : Prompt
    {
        switch (type)
        {
            case PromptType.Exposition:
                return expositionPrompts as T[];

            case PromptType.RisingAction:
                return risingActionPrompts as T[];

            case PromptType.Climax:
                return climaxPrompts as T[];

            case PromptType.Resolution:
                return resolutionPrompts as T[];

            default:
                return System.Array.Empty<T>();
        }
    }

    public void LoadPrompts()
    {
        expositionPrompts = Resources.LoadAll<ExpositionPrompt>("Scriptables/Prompts/EXP");
        risingActionPrompts = Resources.LoadAll<RisingActionPrompt>("Scriptables/Prompts/RA");
        climaxPrompts = Resources.LoadAll<ClimaxPrompt>("Scriptables/Prompts/CLX");
        resolutionPrompts = Resources.LoadAll<ResolutionPrompt>("Scriptables/Prompts/RES");

        Debug.Log("PromptManager: Prompts loaded successfully.");
    }
}
