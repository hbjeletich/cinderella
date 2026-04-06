using UnityEngine;
using TMPro;

public class ScoreCard : MonoBehaviour
{
    public TextMeshProUGUI placeText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(int place, Player player)
    {
        placeText.text = GetOrdinal(place);
        nameText.text = player.playerName;
        scoreText.text = $"{player.score} pts";
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.8f;
    }

    public void Reveal()
    {
        StartCoroutine(RevealCoroutine());
    }

    private System.Collections.IEnumerator RevealCoroutine()
    {
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            canvasGroup.alpha = ease;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, ease);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    private string GetOrdinal(int n)
    {
        switch(n)
        {
            case 1:
                return "1st";
            case 2:
                return "2nd";
            case 3:
                return "3rd";
        };
        return $"{n}th";
    }
}