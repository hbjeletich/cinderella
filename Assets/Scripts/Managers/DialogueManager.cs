using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private Dictionary<string, string> dialogues = new Dictionary<string, string>();

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDialogues();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDialogues()
    {
        Debug.Log("DialogueManager: Loading dialogues");
        // load from resources
        TextAsset csv = Resources.Load<TextAsset>("Dialogues/story_text");

        if(csv == null)
        {
            Debug.LogError("DialogueManager: Could not find story_text file!");
        }

        string[] lines = csv.text.Split("\n");

        for(int i=1; i < lines.Length; i++)
        {
            if(string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] fields = ParseCSVLine(lines[i]);

            if(fields.Length >= 2)
            {
                string key = fields[0].Trim();
                string text = fields[1].Trim();

                // remove surrounding quotes
                if(text.StartsWith("\"") && text.EndsWith("\""))
                {
                    text = text.Substring(1, text.Length - 2);
                }

                dialogues[key] = text;
            }
        }

        Debug.Log($"DialogueManager: Loaded {dialogues.Count} dialogue segments.");
    }

    public static string[] ParseCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim('"'));
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        fields.Add(currentField.Trim('"'));
        
        return fields.ToArray();
    }

    public string GetDialogue(string key)
    {
        if(dialogues.TryGetValue(key, out string text))
        {
            return text;
        }

        Debug.LogWarning($"DialogueManager: Dialogue key '{key}' not found!");
        return "Missing dialogue!";
    }

    public bool HasDialogue(string key)
    {
        return dialogues.ContainsKey(key);
    }
}
