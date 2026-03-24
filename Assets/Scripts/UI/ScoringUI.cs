using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ScoringUI : BaseGameUI
{
    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        StartCoroutine(ShowScoreboardCoroutine(roundNumber, sortedPlayers, onComplete));
    }

    private IEnumerator ShowScoreboardCoroutine(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        string header = $"Round {roundNumber} Complete!";
        ChangeText(header);
        yield return new WaitForSeconds(baseTextTime);

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            int place = sortedPlayers.Count - i;
            string label = GetOrdinal(place);
            Player player = sortedPlayers[i];
            string line = $"{label}: {player.playerName} — {player.score} pts";
            ChangeText(line);
            yield return new WaitForSeconds(CalculateDisplayTime(line));
        }

        ClearText();
        onComplete?.Invoke();
    }

    private string GetOrdinal(int n)
    {
        switch (n)
        {
            case 1: return $"{n}st";
            case 2: return $"{n}nd";
            case 3: return $"{n}rd";
            default: return $"{n}th";
        }
    }
}