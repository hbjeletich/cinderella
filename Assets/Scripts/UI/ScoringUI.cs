using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ScoringUI : BaseGameUI
{
    [Header("Score Cards")]
    public GameObject scoreCardPrefab;
    public Transform cardContainer;

    [Header("Round Scoreboard Timing")]
    public float cascadeDelay = 0.15f;
    public float lingerTime = 4f;

    [Header("Final Scoreboard Timing")]
    public float finalCascadeDelay = 0.6f;
    public float finalLingerTime = 6f;
    public float countdownTime = 10f;

    private List<GameObject> activeCards = new List<GameObject>();

    public void ShowScoreboard(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        StartCoroutine(ShowScoreboardCoroutine(roundNumber, sortedPlayers, onComplete));
    }

    public void ShowFinalScoreboard(List<Player> sortedPlayers, float holdTime, Action onComplete)
    {
        StartCoroutine(ShowFinalScoreboardCoroutine(sortedPlayers, holdTime, onComplete));
    }

    private IEnumerator ShowScoreboardCoroutine(int roundNumber, List<Player> sortedPlayers, Action onComplete)
    {
        ClearCards();

        ChangeText($"Round {roundNumber} Complete!");
        yield return new WaitForSeconds(baseTextTime);
        ClearText();

        // from last place to first
        yield return StartCoroutine(CascadeCards(sortedPlayers, cascadeDelay));

        yield return new WaitForSeconds(lingerTime);

        ClearCards();
        ClearText();
        onComplete?.Invoke();
    }

    private IEnumerator ShowFinalScoreboardCoroutine(List<Player> sortedPlayers, float holdTime, Action onComplete)
    {
        ClearCards();

        ChangeText("Final Scores!");
        yield return new WaitForSeconds(baseTextTime);
        ClearText();

        // slower for drama
        yield return StartCoroutine(CascadeCards(sortedPlayers, finalCascadeDelay));

        // winner callout
        Player winner = sortedPlayers[sortedPlayers.Count - 1];
        ChangeText($"{winner.playerName} wins!");
        yield return new WaitForSeconds(finalLingerTime);

        // return to lobby countdown
        for (int i = (int)countdownTime; i > 0; i--)
        {
            ChangeText($"Returning to lobby in {i}...");
            yield return new WaitForSeconds(1f);
        }

        ClearCards();
        ClearText();
        onComplete?.Invoke();
    }

    private IEnumerator CascadeCards(List<Player> sortedPlayers, float delay)
    {
        List<ScoreCard> cards = new List<ScoreCard>();

        for (int i = sortedPlayers.Count - 1; i >= 0; i--)
        {
            int place = sortedPlayers.Count - i;
            GameObject cardObj = Instantiate(scoreCardPrefab, cardContainer);
            activeCards.Add(cardObj);

            ScoreCard card = cardObj.GetComponent<ScoreCard>();
            card.Setup(place, sortedPlayers[i]);
            card.Hide();
            cards.Add(card);
        }

        // reveal bottom-to-top (last place first, 1st place last)
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            cards[i].Reveal();
            yield return new WaitForSeconds(delay);
        }
    }

    private void ClearCards()
    {
        foreach (GameObject card in activeCards)
        {
            if (card != null) Destroy(card);
        }
        activeCards.Clear();
    }

    public override void Deactivate()
    {
        ClearCards();
        base.Deactivate();
    }
}