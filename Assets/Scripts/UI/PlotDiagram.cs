using UnityEngine;
using UnityEngine.UI;

public class PlotDiagram : MonoBehaviour
{
    [Header("UI References")]
    public Image filledImage;
    public RectTransform handle;

    [Header("Paths")]
    public RectTransform[] pathPoints;

    [Header("Fill")]
    [Range(0f, 1f)]
    public float fillAmount;

    private float minX;
    private float maxX;
    private Vector2[] waypoints;

    private void Awake()
    {
        SaveXRange();
    }

    private void OnValidate()
    {
        SaveXRange();
        SetFillAmount(fillAmount);
    }

    public void SetFillAmount(float amount)
    {
        fillAmount = Mathf.Clamp01(amount);

        if (filledImage != null)
        {
            filledImage.fillAmount = fillAmount;
        }

        UpdateHandlePosition();
    }

    public float GetFillAmount() => fillAmount;

    private void SaveXRange()
    {
        waypoints = new Vector2[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            waypoints[i] = pathPoints[i].anchoredPosition;
        }

        if (waypoints == null || waypoints.Length < 2)
            return;

        minX = waypoints[0].x;
        maxX = waypoints[0].x;

        for (int i = 1; i < waypoints.Length; i++)
        {
            minX = Mathf.Min(minX, waypoints[i].x);
            maxX = Mathf.Max(maxX, waypoints[i].x);
        }
    }

    private Vector2 GetPositionAtX(float targetX)
    {
        // find which segment contains this X and lerp along it
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            float x0 = waypoints[i].x;
            float x1 = waypoints[i + 1].x;

            // check if targetX falls within this segment's X range
            if ((x0 <= targetX && targetX <= x1) || (x1 <= targetX && targetX <= x0))
            {
                float segXLen = x1 - x0;
                if (Mathf.Abs(segXLen) < 0.001f)
                    return waypoints[i];

                float t = (targetX - x0) / segXLen;
                return Vector2.Lerp(waypoints[i], waypoints[i + 1], t);
            }
        }

        return waypoints[waypoints.Length - 1];
    }

    private void UpdateHandlePosition()
    {
        if(handle == null || waypoints == null || waypoints.Length < 2)
            return;

        if (Mathf.Abs(maxX - minX) < 0.001f)
            return;

        // map fill amount to an X position — matches horizontal fill direction
        float targetX = Mathf.Lerp(minX, maxX, fillAmount);
        handle.anchoredPosition = GetPositionAtX(targetX);
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null)
            return;

        Gizmos.color = Color.blue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 worldPos = rt.TransformPoint(waypoints[i]);
            Gizmos.DrawSphere(worldPos, 5f);

            if (i > 0)
            {
                Vector3 prevWorldPos = rt.TransformPoint(waypoints[i - 1]);
                Gizmos.DrawLine(prevWorldPos, worldPos);
            }
        }
    }
}