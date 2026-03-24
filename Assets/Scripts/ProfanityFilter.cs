using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ProfanityFilter : MonoBehaviour
{
    public static ProfanityFilter Instance { get; private set; }

    [Header("Settings")]
    public TextAsset wordListFile;
    public char censorCharacter = '*';

    private HashSet<string> bannedWords = new HashSet<string>();

    // this is a pattern matcher that finds banned words inside text
    // it's built once on startup so checking is fast during gameplay
    private Regex wordMatcher;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadWordList();
    }

    private void LoadWordList()
    {
        if(wordListFile == null)
        {
            Debug.LogWarning("ProfanityFilter: No word list file assigned!");
            return;
        }

        string[] lines = wordListFile.text.Split('\n');
        List<string> searchTerms = new List<string>();

        foreach(string line in lines)
        {
            string word = line.Trim().ToLower();
            if(string.IsNullOrEmpty(word)) continue;

            bannedWords.Add(word);
            // escape any special characters in the word so they're treated as plain text
            searchTerms.Add(Regex.Escape(word));
        }

        if(searchTerms.Count > 0)
        {
            // combine all banned words into one search pattern
            // \b means "word boundary" — so "hell" won't match inside "hello"
            // IgnoreCase means it catches "DAMN", "Damn", "damn", etc.
            string combinedPattern = @"\b(" + string.Join("|", searchTerms) + @")\b";
            wordMatcher = new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        Debug.Log($"ProfanityFilter: Loaded {bannedWords.Count} banned words.");
    }

    public string Censor(string input)
    {
        if(wordMatcher == null || string.IsNullOrEmpty(input))
            return input;

        return wordMatcher.Replace(input, match =>
        {
            return new string(censorCharacter, match.Value.Length);
        });
    }

    public bool ContainsProfanity(string input)
    {
        if(wordMatcher == null || string.IsNullOrEmpty(input))
            return false;

        return wordMatcher.IsMatch(input);
    }
}