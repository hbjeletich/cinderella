using UnityEngine;
using UnityEngine.EventSystems;

public class GrowButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Grow/Shrink Settings")]
    public Vector3 targetScale = new Vector3(1.2f, 1.2f, 1.2f);
    public float growSpeed = 5f;

    [Header("Optional Z Rotation")]
    public bool rotateOnHover = false;
    public float rotationAngle = 15f;
    public float rotationSpeed = 5f;

    private Vector3 originalScale;
    private Vector3 originalRotation;
    private bool isGrowing = false;
    
    private void Start()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (isGrowing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * growSpeed);
            if (rotateOnHover)
            {
                // rotates back and forth between -rotationAngle and rotationAngle
                float zRotation = rotationAngle * Mathf.Sin(Time.time * rotationSpeed);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, zRotation);
            }
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * growSpeed);
            if (rotateOnHover)
            {
                // reset rotation when not hovering
                float currentZRotation = transform.localEulerAngles.z;
                float newZRotation = Mathf.LerpAngle(currentZRotation, 0f, Time.deltaTime * rotationSpeed);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, newZRotation);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isGrowing = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isGrowing = false;
    }
}
