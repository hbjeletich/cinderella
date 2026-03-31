using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class AnswerCard : MonoBehaviour
{
    [Header("UI References")]
    public Image scrollBackground;
    public TextMeshProUGUI answerText;
    public Image playerIconImage;
    public TextMeshProUGUI playerNameText;
    public RectTransform emoteContainer;

    [Header("Emote Prefab")]
    public GameObject emotePrefab;

    [Header("Animation")]
    public float popInDuration = 0.4f;
    public float popInStartScale = 2f;
    public float dismissDuration = 0.3f;
    public float winnerScale = 2f;
    public float winnerDuration = 0.4f;
    public float authorFadeDuration = 0.3f;

    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private AnswerMagicText answerMagicText;
    private List<GameObject> spawnedEmotes = new List<GameObject>();

    private void Awake()
    {
        rt = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (answerText != null)
        {
            answerMagicText = answerText.GetComponent<AnswerMagicText>();
            if (answerMagicText == null)
                answerMagicText = answerText.gameObject.AddComponent<AnswerMagicText>();
            answerText.color = Color.black;
        }

        HideAuthor();
    }

    public void SetAnswer(string text)
    {
        if (answerMagicText != null)
            answerMagicText.ShowInstant(text);
        else if (answerText != null)
            answerText.text = text;
    }

    public void RevealAnswer(string text, System.Action onComplete = null)
    {
        if (answerMagicText != null)
            answerMagicText.Reveal(text, onComplete);
        else
        {
            SetAnswer(text);
            onComplete?.Invoke();
        }
    }

    public void PopIn(Vector2 targetPos, TweenCallback onComplete = null)
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one * popInStartScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack));
        seq.Join(rt.DOAnchorPos(targetPos, popInDuration).SetEase(Ease.OutBack));
        if (onComplete != null)
            seq.OnComplete(onComplete);
    }

    public void PopInBig(TweenCallback onComplete = null)
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.zero;

        rt.DOScale(Vector3.one * winnerScale, popInDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(onComplete);
    }

    public void PopToBig(TweenCallback onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(Vector3.one * winnerScale, winnerDuration).SetEase(Ease.OutBack));
        seq.Join(rt.DOAnchorPos(Vector2.zero, winnerDuration).SetEase(Ease.OutBack));
        if (onComplete != null)
            seq.OnComplete(onComplete);
    }

    public void Dismiss(TweenCallback onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(Vector3.zero, dismissDuration).SetEase(Ease.InBack));
        seq.Join(canvasGroup.DOFade(0f, dismissDuration));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void RevealAuthor(Player player)
    {
        if (player == null || player.playerIcon == null) return;

        if (playerIconImage != null)
        {
            playerIconImage.gameObject.SetActive(true);
            playerIconImage.sprite = player.playerIcon.playerIcon;
            playerIconImage.color = new Color(1, 1, 1, 0);
            playerIconImage.DOFade(1f, authorFadeDuration);
        }

        if (playerNameText != null)
        {
            playerNameText.gameObject.SetActive(true);
            string displayName = player.playerName;
            if (displayName.Length > 10)
                displayName = displayName.Substring(0, 10) + "...";
            playerNameText.text = displayName;
            playerNameText.alpha = 0f;
            playerNameText.DOFade(1f, authorFadeDuration);
        }
    }

    public void ShowEmotes(Dictionary<Player, Reaction> reactions, ReactionData reactionData,
        float waveDelay = 0.3f, float baseScale = 0.6f, float bonusScale = 0.9f)
    {
        ClearEmotes();

        if (reactions == null || reactions.Count == 0 || reactionData == null) return;

        int totalReactors = reactions.Count(r => r.Value.reactionType != ReactionType.None);
        if (totalReactors == 0) return;

        var grouped = reactions
            .Where(r => r.Value.reactionType != ReactionType.None)
            .GroupBy(r => r.Value.reactionType)
            .OrderBy(g => g.Count())
            .ToList();

        float spacing = 150f;
        float totalWidth = (totalReactors - 1) * spacing;
        float startX = -totalWidth / 2f;
        float yOffset = -100f;
        float maxRotation = 20f;
        float yJitter = 10f;

        int emoteIndex = 0;
        int waveIndex = 0;

        foreach (var group in grouped)
        {
            int groupSize = group.Count();
            float targetScale = baseScale + ((float)groupSize / totalReactors) * bonusScale;
            float delay = waveIndex * waveDelay;

            foreach (var kvp in group)
            {
                Sprite emoteSprite = reactionData.GetSprite(kvp.Value.reactionType);
                if (emoteSprite == null) continue;

                GameObject emoteObj = Instantiate(emotePrefab, emoteContainer);
                spawnedEmotes.Add(emoteObj);

                Image emoteImage = emoteObj.GetComponent<Image>();
                if (emoteImage != null)
                    emoteImage.sprite = emoteSprite;

                RectTransform emoteRT = emoteObj.GetComponent<RectTransform>();

                float x = startX + emoteIndex * spacing;
                float y = yOffset + Random.Range(-yJitter, yJitter);
                emoteRT.anchoredPosition = new Vector2(x, y);

                float rotation = Random.Range(-maxRotation, maxRotation);
                emoteRT.localRotation = Quaternion.Euler(0f, 0f, rotation);

                emoteRT.localScale = Vector3.zero;
                emoteRT.DOScale(Vector3.one * targetScale, 0.3f)
                    .SetEase(Ease.OutBack, 2f)
                    .SetDelay(delay);

                emoteIndex++;
            }
            waveIndex++;
        }
    }

    public void ClearEmotes()
    {
        foreach (var emote in spawnedEmotes)
            if (emote != null) Destroy(emote);
        spawnedEmotes.Clear();
    }

    public void HideAuthor()
    {
        if (playerIconImage != null)
            playerIconImage.gameObject.SetActive(false);
        if (playerNameText != null)
            playerNameText.gameObject.SetActive(false);
    }

    public void Reset()
    {
        if(rt == null) rt = GetComponent<RectTransform>();
        if(canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        ClearEmotes();
        HideAuthor();
        if (answerMagicText != null)
            answerMagicText.Clear();
        else if (answerText != null)
            answerText.text = "";
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
        canvasGroup.alpha = 1f;
        gameObject.SetActive(false);
    }
}