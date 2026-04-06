using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using DG.Tweening;

public class RevealingUI : BaseGameUI
{
    [Header("Question Display")]
    public RectTransform questionContainer;
    public TextMeshProUGUI questionText;
    public float questionPopDuration = 0.4f;
    public float questionStartScale = 2f;
    public float questionPadding = 40f;

    [Header("Answer Cards")]
    public GameObject answerCardPrefab;
    public RectTransform cardGridContainer;   // GridLayoutGroup, 2 col, ContentSizeFitter
    public RectTransform singleCardParent;    // empty parent centered on screen for solo cards
    public float cardStaggerDelay = 0.3f;

    [Header("Reaction Data")]
    public ReactionData reactionData;

    [Header("Timing")]
    public float postRevealHoldTime = 2.5f;
    public float postEmoteHoldTime = 2f;

    [Header("Card Layout")]
    public float maxCardHeight = 300f;
    public float xPadding = 150f;
    public float xSpacing = 0f;

    private Vector2 questionRestPos;
    private bool hasStoredPositions;
    private Coroutine activeSequence;
    private MagicText questionMagicText;
    private bool winnerWasPopped;

    private List<AnswerCard> spawnedCards = new List<AnswerCard>();

    protected override void Awake()
    {
        base.Awake();

        if (questionContainer != null)
            questionContainer.gameObject.SetActive(false);

        if (questionText != null)
            questionMagicText = questionText.GetComponent<MagicText>();

        if (cardGridContainer != null)
            cardGridContainer.gameObject.SetActive(false);
    }

    private void StoreQuestionPos()
    {
        if (hasStoredPositions) return;
        if (questionContainer != null)
            questionRestPos = questionContainer.anchoredPosition;
        hasStoredPositions = true;
    }

    public override void Activate()
    {
        base.Activate();
        StoreQuestionPos();
    }

    public override void Deactivate()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }

        questionMagicText?.Stop();

        if (questionContainer != null)
        {
            questionContainer.DOKill();
            questionContainer.gameObject.SetActive(false);
        }

        ClearCards();
        HideText();
    }

    // --- Public API ---

    public void ShowSubmission(Player player, string answer, Action onComplete, string promptText = null)
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(SubmissionSequence(player, answer, onComplete, promptText));
    }

    public void ShowOptions(Player player, List<string> answers, Action onComplete, string promptText = null)
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(OptionsSequence(answers, onComplete, promptText));
    }

    public void ShowInPhaseNarration(string text, Action onComplete)
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(InPhaseNarrationSequence(text, onComplete));
    }

    public void RevealWinnerCard(string winningAnswer, Action onComplete)
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(RevealWinnerCardSequence(winningAnswer, onComplete));
    }

    public void ShowReactionsAndAuthor(string winningAnswer, Player author,
        Dictionary<Player, Reaction> reactions, Action onComplete)
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        activeSequence = StartCoroutine(ReactionsAndAuthorSequence(winningAnswer, author, reactions, onComplete));
    }

    // --- Sequences ---

    private IEnumerator InPhaseNarrationSequence(string text, Action onComplete)
    {
        ResetAll();

        yield return PopInQuestion(text);
        yield return new WaitForSeconds(postRevealHoldTime);

        onComplete?.Invoke();
        activeSequence = null;
    }

    private IEnumerator SubmissionSequence(Player player, string answer, Action onComplete, string promptText)
    {
        StoreQuestionPos();
        ResetAll();

        if (!string.IsNullOrEmpty(promptText))
            yield return PopInQuestion(promptText);

        // Single card — spawn into singleCardParent so it's centered
        AnswerCard card = SpawnCard(singleCardParent);

        bool popDone = false;
        card.PopInBig(() => popDone = true);
        yield return new WaitUntil(() => popDone);

        bool revealDone = false;
        card.RevealAnswer(answer, () => revealDone = true);
        yield return new WaitUntil(() => revealDone);

        yield return new WaitForSeconds(postRevealHoldTime);

        onComplete?.Invoke();
        activeSequence = null;
    }

    private IEnumerator OptionsSequence(List<string> answers, Action onComplete, string promptText)
    {
        StoreQuestionPos();
        ResetAll();

        if (!string.IsNullOrEmpty(promptText))
            yield return PopInQuestion(promptText);

        cardGridContainer.gameObject.SetActive(true);

        // Manual layout — no GridLayoutGroup needed
        float containerW = cardGridContainer.rect.width;
        float containerH = cardGridContainer.rect.height;
        float xPadding = 150f;
        //float yPadding = 20f;
        //float ySpacing = 16f;
        float xSpacing = 16f;

        int columns = answers.Count > 6 ? 3 : 2;
        int rows = Mathf.CeilToInt((float)answers.Count / columns);

        float usableW = containerW - (xPadding * 2) - (xSpacing * (columns - 1));
        float usableH = containerH;

        float cellW = usableW / columns;
        float cellH = usableH / rows;

        // Cap cell height to original prefab height if there's plenty of room
        cellH = Mathf.Min(cellH, maxCardHeight);

        // Total grid dimensions (for centering)
        float gridW = (cellW * columns) + (xSpacing * (columns - 1));
        float gridH = (cellH * rows);

        float startX = -gridW / 2f + cellW / 2f;
        float startY = (cellH / 2f);

        for (int i = 0; i < answers.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float x = startX + col * (cellW + xSpacing);
            float y = startY - row * cellH;

            AnswerCard card = SpawnCard(cardGridContainer);
            card.gameObject.SetActive(true);

            RectTransform cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(cellW, cellH);
            cardRT.anchoredPosition = new Vector2(x, y);

            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cardRT.localScale = Vector3.one * 0.5f;

            DOTween.Sequence()
                .Append(cardRT.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack))
                .Join(cg.DOFade(1f, 0.2f));

            bool revealDone = false;
            card.RevealAnswer(answers[i], () => revealDone = true);
            yield return new WaitUntil(() => revealDone);

            if (i < answers.Count - 1)
                yield return new WaitForSeconds(cardStaggerDelay);
        }

        onComplete?.Invoke();
        activeSequence = null;
    }

    private IEnumerator RevealWinnerCardSequence(string winningAnswer, Action onComplete)
    {
        AnswerCard winnerCard = null;
        List<AnswerCard> loserCards = new List<AnswerCard>();

        foreach (var card in spawnedCards)
        {
            if (!card.gameObject.activeSelf) continue;

            if (card.answerText.text == winningAnswer && winnerCard == null)
                winnerCard = card;
            else
                loserCards.Add(card);
        }

        if (winnerCard == null)
        {
            winnerWasPopped = false;
            onComplete?.Invoke();
            yield break;
        }

        winnerWasPopped = loserCards.Count > 0;

        // Pull winner out of grid so layout doesn't fight the tween
        if (winnerWasPopped)
        {
            RectTransform winnerRT = winnerCard.GetComponent<RectTransform>();
            winnerRT.SetParent(singleCardParent, true);
        }

        // Hide grid
        if (cardGridContainer != null)
            cardGridContainer.gameObject.SetActive(false);

        // Dismiss losers
        if (winnerWasPopped)
        {
            int dismissedCount = 0;
            int totalLosers = loserCards.Count;
            foreach (var loser in loserCards)
                loser.Dismiss(() => dismissedCount++);
            yield return new WaitUntil(() => dismissedCount >= totalLosers);

            // Pop winner to center big
            bool bigDone = false;
            winnerCard.PopToBig(() => bigDone = true);
            yield return new WaitUntil(() => bigDone);
        }

        yield return new WaitForSeconds(0.3f);

        onComplete?.Invoke();
        activeSequence = null;
    }

    private IEnumerator ReactionsAndAuthorSequence(string winningAnswer, Player author,
        Dictionary<Player, Reaction> reactions, Action onComplete)
    {
        AnswerCard winnerCard = null;

        foreach (var card in spawnedCards)
        {
            if (!card.gameObject.activeSelf) continue;
            if (card.answerText.text == winningAnswer && winnerCard == null)
                winnerCard = card;
        }

        if (winnerCard == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // Emotes
        bool hasReactions = reactions != null &&
            reactions.Values.Any(r => r.reactionType != ReactionType.None);

        if (hasReactions && reactionData != null)
        {
            winnerCard.ShowEmotes(reactions, reactionData);

            int waveCount = reactions.Values
                .Where(r => r.reactionType != ReactionType.None)
                .Select(r => r.reactionType)
                .Distinct()
                .Count();
            yield return new WaitForSeconds(waveCount * 0.3f + 0.5f);
        }

        // Author
        if (author != null)
        {
            winnerCard.RevealAuthor(author);
            yield return new WaitForSeconds(postEmoteHoldTime);
        }
        else
        {
            yield return new WaitForSeconds(postRevealHoldTime);
        }

        onComplete?.Invoke();
        activeSequence = null;
    }

    // --- Question ---

    private IEnumerator PopInQuestion(string text)
    {
        questionContainer.gameObject.SetActive(true);

        if (questionText != null)
            questionText.text = "";

        var cg = questionContainer.GetComponent<CanvasGroup>();
        if (cg == null) cg = questionContainer.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        questionContainer.anchoredPosition = Vector2.zero;
        questionContainer.localScale = Vector3.one * questionStartScale;

        bool moveDone = false;
        Sequence seq = DOTween.Sequence();
        seq.Append(questionContainer.DOScale(Vector3.one, questionPopDuration).SetEase(Ease.OutBack));
        seq.Join(questionContainer.DOAnchorPos(questionRestPos, questionPopDuration).SetEase(Ease.OutBack));
        seq.OnComplete(() => moveDone = true);

        yield return new WaitUntil(() => moveDone);

        if (questionMagicText != null)
        {
            bool revealDone = false;
            questionMagicText.Reveal(text, () => revealDone = true);
            yield return new WaitUntil(() => revealDone);
        }
        else if (questionText != null)
        {
            questionText.text = text;
        }

        FitQuestionContainer();

        yield return new WaitForSeconds(0.2f);
    }

    private void FitQuestionContainer()
    {
        if (questionText == null || questionContainer == null) return;

        questionText.ForceMeshUpdate();
        float preferredHeight = questionText.preferredHeight + questionPadding;

        Vector2 size = questionContainer.sizeDelta;
        size.y = Mathf.Max(size.y, preferredHeight);
        questionContainer.sizeDelta = size;
    }

    // --- Card Management ---

    private AnswerCard SpawnCard(RectTransform parent)
    {
        GameObject cardObj = Instantiate(answerCardPrefab, parent);
        AnswerCard card = cardObj.GetComponent<AnswerCard>();
        spawnedCards.Add(card);
        return card;
    }

    private void ClearCards()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null)
            {
                card.GetComponent<RectTransform>().DOKill();
                Destroy(card.gameObject);
            }
        }
        spawnedCards.Clear();

        if (cardGridContainer != null)
            cardGridContainer.gameObject.SetActive(false);
    }

    private void ResetAll()
    {
        ClearCards();

        if (questionContainer != null)
        {
            questionContainer.DOKill();
            questionContainer.gameObject.SetActive(false);
        }
    }
}