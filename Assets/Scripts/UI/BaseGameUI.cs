using UnityEngine;
using TMPro;

public abstract class BaseGameUI : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public float baseTextTime = 2f;
    public float timePerCharacter = 0.05f;

    protected virtual void Awake()
    {
        HideText();
    }

    protected float CalculateDisplayTime(string text)
    {
        return Mathf.Max(baseTextTime, text.Length * timePerCharacter);
    }

    protected void ChangeText(string text)
    {
        if(displayText == null) return;
        displayText.gameObject.SetActive(true);
        Debug.Log($"{GetType().Name}: Changing text to {text}");
        displayText.text = text;
    }

    protected void ClearText()
    {
        if(displayText != null)
            displayText.text = "";
    }

    protected void HideText()
    {
        if(displayText != null)
        {
            displayText.text = "";
            displayText.gameObject.SetActive(false);
        }
    }

    public virtual void Activate()
    {
        if(displayText != null)
            displayText.gameObject.SetActive(true);
    }

    public virtual void Deactivate()
    {
        StopAllCoroutines();
        HideText();
    }
}