using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows;

public static class CSVReader
{
    [MenuItem("Tools/Import CSV")]
    public static void ImportCSV()
    {
        //path = AssetDatabase.GetAssetPath(chosenCSV);

        string path = CSVUtils.GetSelectedPath();
        Debug.Log($"Parsing {path}");

        if(!AssetDatabase.AssetPathExists(path))
        {
            Debug.Log($"Asset does not exist at {path}");
            return;
        }

        TextAsset csvFile = LoadAssetFromFile(path);
        if(csvFile == null)
        {
            Debug.LogError($"Could not find TextAsset at {path}");
            return;
        }

        string csvString = csvFile.ToString();

        string[] csvLines = csvString.Split("\n");

        // use id(first column) of line 1 to determind which type of prompt we're making
        string[] separateID = csvLines[1].Split("_");
        string promptType = separateID[0];

        // delete all old assets
        Directory.Delete($"Assets/Resources/Scriptables/Prompts/{promptType}");

        for(int i = 1; i <= csvLines.Length - 1; i++)
        {
            string[] row = ParseCSVLine(csvLines[i]);
            switch(promptType)
            {
                // which type of scriptable are we making?
                case("EXP"):
                    CreateExpositionPrompts(row, promptType, i);
                    break;
                case("RA"):
                    CreateRisingActionPrompts(row, promptType, i);
                    break;
                case("CLX"):
                    CreateClimaxPrompt(row, promptType, i);
                    break;
                case("RES"):
                    CreateResolutionPrompt(row, promptType, i);
                    break;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Import successful!");
    }

    public static TextAsset LoadAssetFromFile(string pPath)
    {
        Debug.Log($"Trying to Load asset from file {pPath} ...");
        TextAsset loadedObject = AssetDatabase.LoadAssetAtPath<TextAsset>($"{pPath}");

        if (loadedObject == null)
        {
            Debug.LogError("ERROR ...no file found.");
            return null;
        }

        return loadedObject;
    }

    public static void CreateExpositionPrompts(string[] row, string promptType, int index)
    {
        ExpositionPrompt prompt = ScriptableObject.CreateInstance<ExpositionPrompt>();
        
        prompt.id = row[0];
        
        PromptType myType = prompt.StringToType(promptType);
        if(myType != PromptType.None)
        {
            prompt.type = myType;
        }

        prompt.promptText = row[1];
        bool necessity = false;
        if(row[2].Contains("TRUE"))
        {
            necessity = true;
        }
        prompt.necessity = necessity;
        prompt.storyElement = row[3].Trim('\n', '\r');

        if (!Directory.Exists($"Assets/Resources/Scriptables/Prompts/EXP/"))
        {
            Directory.CreateDirectory($"Assets/Resources/Scriptables/Prompts/EXP/");
        }

        string filePath = $"Assets/Resources/Scriptables/Prompts/EXP/ExpositionPrompt_{index}.asset";
        Debug.Log(filePath);
        AssetDatabase.CreateAsset(prompt, filePath);
    }

    public static void CreateRisingActionPrompts(string[] row, string promptType, int index)
    {
        RisingActionPrompt prompt = ScriptableObject.CreateInstance<RisingActionPrompt>();
        
        prompt.id = row[0];

        PromptType myType = prompt.StringToType(promptType);
        if(myType != PromptType.None)
        {
            prompt.type = myType;
        }

        prompt.promptText = row[1];
        prompt.round = int.Parse(row[2]);
        prompt.storyBeat = row[3];

        string[] options = row[4].Split("\n");
        prompt.options = options;

        prompt.resonanceTag = row[5].Trim('\n', '\r');

        if (!Directory.Exists($"Assets/Resources/Scriptables/Prompts/RA/"))
        {
            Directory.CreateDirectory($"Assets/Resources/Scriptables/Prompts/RA/");
        }

        string filePath = $"Assets/Resources/Scriptables/Prompts/RA/RisingActionPrompt_{index}.asset";
        Debug.Log(filePath);
        AssetDatabase.CreateAsset(prompt, filePath);
    }

    public static void CreateClimaxPrompt(string[] row, string promptType, int index)
    {
        ClimaxPrompt prompt = ScriptableObject.CreateInstance<ClimaxPrompt>();
        
        prompt.id = row[0];

        PromptType myType = prompt.StringToType(promptType);
        if(myType != PromptType.None)
        {
            prompt.type = myType;
        }

        prompt.promptText = row[1];
        prompt.climaxType = row[2];

        prompt.protagonistOptions = row[3].Split(" | ");
        prompt.antagonistOptions = row[4].Split(" | ");

        prompt.antagonistOptions[prompt.antagonistOptions.Count() - 1] = prompt.antagonistOptions[prompt.antagonistOptions.Count() - 1].Trim('\n', '\r');

        if (!Directory.Exists($"Assets/Resources/Scriptables/Prompts/CLX/"))
        {
            Directory.CreateDirectory($"Assets/Resources/Scriptables/Prompts/CLX/");
        }

        string filePath = $"Assets/Resources/Scriptables/Prompts/CLX/ClimaxPrompt_{index}.asset";
        Debug.Log(filePath);
        AssetDatabase.CreateAsset(prompt, filePath);
    }

    public static void CreateResolutionPrompt(string[] row, string promptType, int index)
    {
        ResolutionPrompt prompt = ScriptableObject.CreateInstance<ResolutionPrompt>();
        
        prompt.id = row[0];

        PromptType myType = prompt.StringToType(promptType);
        if(myType != PromptType.None)
        {
            prompt.type = myType;
        }

        prompt.promptText = row[1];
        Debug.Log($"Final text: {prompt.promptText}");

        prompt.outcomeCategory = row[row.Count() - 2];
        prompt.tone = row[row.Count() - 1].Trim('\n', '\r');

        if (!Directory.Exists($"Assets/Resources/Scriptables/Prompts/RES/"))
        {
            Directory.CreateDirectory($"Assets/Resources/Scriptables/Prompts/RES/");
        }

        string filePath = $"Assets/Resources/Scriptables/Prompts/RES/ResolutionPrompt_{index}.asset";
        Debug.Log(filePath);
        AssetDatabase.CreateAsset(prompt, filePath);
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
    
}
