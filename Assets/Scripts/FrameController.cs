using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FrameController : MonoBehaviour
{
    public PhaseColorConfig colorConfig;

    private int _frameMainColorId;
    private int _frameAccentColorId;
    private int _frameDetailColorId;
    private int _frameFineDetailColorId;
    private Image image;

    private Material mat;
    void Awake()
    {
        _frameAccentColorId = Shader.PropertyToID("_FrameAccentColor");
        _frameMainColorId = Shader.PropertyToID("_FrameMainColor");
        _frameDetailColorId = Shader.PropertyToID("_eDetailColor");
        _frameFineDetailColorId = Shader.PropertyToID("_CenterDetailColor");

        image = GetComponent<Image>();
        if (image != null)
            mat = image.material;
    }

    void Start()
    {
        if (colorConfig == null || colorConfig.phases.Length == 0) return;

        var first = colorConfig.phases[0];
        mat.SetColor(_frameMainColorId, first.mainColor);
        mat.SetColor(_frameAccentColorId, first.accentColor);
        mat.SetColor(_frameDetailColorId, first.detailColor);
        mat.SetColor(_frameFineDetailColorId, first.fineDetailColor);
    }

    public void UpdateFrameColors(string phaseName, float duration)
    {
        var entry = colorConfig.GetPhase(phaseName);
        if (entry == null)
        {
            Debug.LogWarning("No phase found with name: " + phaseName);
            return;
        }

        StartCoroutine(AnimateFrameColors(entry, duration));
    }

    private IEnumerator AnimateFrameColors(PhaseColorConfig.PhaseEntry entry, float duration)
    {
        Color startMain = mat.GetColor(_frameMainColorId);
        Color startAccent = mat.GetColor(_frameAccentColorId);
        Color startDetail = mat.GetColor(_frameDetailColorId);
        Color startFineDetail = mat.GetColor(_frameFineDetailColorId);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            mat.SetColor(_frameMainColorId, Color.Lerp(startMain, entry.mainColor, t));
            mat.SetColor(_frameAccentColorId, Color.Lerp(startAccent, entry.accentColor, t));
            mat.SetColor(_frameDetailColorId, Color.Lerp(startDetail, entry.detailColor, t));
            mat.SetColor(_frameFineDetailColorId, Color.Lerp(startFineDetail, entry.fineDetailColor, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ensure final colors are set
        mat.SetColor(_frameMainColorId, entry.mainColor);
        mat.SetColor(_frameAccentColorId, entry.accentColor);
        mat.SetColor(_frameDetailColorId, entry.detailColor);
        mat.SetColor(_frameFineDetailColorId, entry.fineDetailColor);
    }
}
