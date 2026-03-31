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

    [Header("Answer Cards (pre-placed in scene)")]
    public AnswerCard[] cardPool;
    public float cardStaggerDelay = 0.3f;

    [Header("Reaction Data")]
    public ReactionData reactionData;

    [Header("Timing")]
    public float postRevealHoldTime = 2.5f;
    public float postEmoteHoldTime = 2f;

    private Vector2 questionRestPos;
    private Vector2[] cardRestPositions;
    private bool hasStoredPositions;
    private Coroutine activeSequence;
    private MagicText questionMagicText;
    private bool winnerWasPopped;

    protected override void Awake()
    {
        base.Awake();

        cardRestPositions = new Vector2[cardPool.Length];
        for (int i = 0; i < cardPool.Length; i++)
        {
            if (cardPool[i] != null)
            {
                cardRestPositions[i] = cardPool[i].GetComponent<RectTransform>().anchoredPosition;
                cardPool[i].gameObject.SetActive(false);
            }
        }

        if (questionContainer != null)
            questionContainer.gameObject.SetActive(false);

        if (questionText != null)
            questionMagicText = questionText.GetComponent<MagicText>();
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

        foreach (var card in cardPool)
        {
            if (card != null)
            {
                card.GetComponent<RectTransform>().DOKill();
                card.Reset();
            }
        }

        HideText();
    }

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

        AnswerCard card = cardPool[0];

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

        int count = Mathf.Min(answers.Count, cardPool.Length);

        for (int i = 0; i < count; i++)
        {
            AnswerCard card = cardPool[i];
            bool popDone = false;

            if (count == 1)
                card.PopInBig(() => popDone = true);
            else
                card.PopIn(cardRestPositions[i], () => popDone = true);

            yield return new WaitUntil(() => popDone);

            bool revealDone = false;
            card.RevealAnswer(answers[i], () => revealDone = true);
            yield return new WaitUntil(() => revealDone);

            if (i < count - 1)
                yield return new WaitForSeconds(cardStaggerDelay);
        }

        onComplete?.Invoke();
        activeSequence = null;
    }

    private IEnumerator RevealWinnerCardSequence(string winningAnswer, Action onComplete)
    {
        AnswerCard winnerCard = null;
        List<AnswerCard> loserCards = new List<AnswerCard>();

        for (int i = 0; i < cardPool.Length; i++)
        {
            if (!cardPool[i].gameObject.activeSelf) continue;

            if (cardPool[i].answerText.text == winningAnswer && winnerCard == null)
                winnerCard = cardPool[i];
            else
                loserCards.Add(cardPool[i]);
        }

        if (winnerCard == null)
        {
            winnerWasPopped = false;
            onComplete?.Invoke();
            yield break;
        }

        winnerWasPopped = loserCards.Count > 0;

        // dismiss losers (don't touch the question — leave it as-is)
        if (winnerWasPopped)
        {
            int dismissedCount = 0;
            int totalLosers = loserCards.Count;
            foreach (var loser in loserCards)
                loser.Dismiss(() => dismissedCount++);
            yield return new WaitUntil(() => dismissedCount >= totalLosers);
        }

        // pop winner to center at 2x (skip if it's the only card)
        if (winnerWasPopped)
        {
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
        // find the winner card (should already be active and visible)
        AnswerCard winnerCard = null;
        int winnerIndex = -1;

        for (int i = 0; i < cardPool.Length; i++)
        {
            if (!cardPool[i].gameObject.activeSelf) continue;

            if (cardPool[i].answerText.text == winningAnswer && winnerCard == null)
            {
                winnerCard = cardPool[i];
                winnerIndex = i;
            }
        }

        if (winnerCard == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // emotes
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

        // reveal author
        if (author != null)
        {
            winnerCard.RevealAuthor(author);
            yield return new WaitForSeconds(postEmoteHoldTime);
        }
        else
        {
            yield return new WaitForSeconds(postRevealHoldTime);
        }

        // animate card back to its rest position (only if it was popped big)
        if (winnerWasPopped && winnerIndex >= 0)
        {
            winnerCard.ClearEmotes();
            RectTransform rt = winnerCard.GetComponent<RectTransform>();
            bool returnDone = false;
            Sequence returnSeq = DOTween.Sequence();
            returnSeq.Append(rt.DOScale(Vector3.one, 0.4f).SetEase(Ease.InOutBack));
            returnSeq.Join(rt.DOAnchorPos(cardRestPositions[winnerIndex], 0.4f).SetEase(Ease.InOutBack));
            returnSeq.OnComplete(() => returnDone = true);
            yield return new WaitUntil(() => returnDone);
            yield return new WaitForSeconds(0.3f);
        }

        onComplete?.Invoke();
        activeSequence = null;
    }

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

    private void ResetAll()
    {
        for (int i = 0; i < cardPool.Length; i++)
        {
            if (cardPool[i] != null)
            {
                cardPool[i].Reset();
                cardPool[i].GetComponent<RectTransform>().anchoredPosition = cardRestPositions[i];
            }
        }

        if (questionContainer != null)
        {
            questionContainer.DOKill();
            questionContainer.gameObject.SetActive(false);
        }
    }
}