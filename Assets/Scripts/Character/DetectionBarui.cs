using UnityEngine;
using UnityEngine.UI;

public class DetectionBarUI : MonoBehaviour
{
    [Header("Detection Bar")]
    [SerializeField] private Image detectionFill;
    [SerializeField] private Color detectionSafeColor = Color.yellow;
    [SerializeField] private Color detectionDangerColor = Color.red;
    [SerializeField] private CanvasGroup detectionGroup;
    [SerializeField] private float fadeSpeed = 3f;

    [Header("Corruption Bar")]
    [SerializeField] private Image corruptionFill;
    [SerializeField] private Color corruptionLowColor = new Color(0.5f, 0f, 0.8f);
    [SerializeField] private Color corruptionHighColor = new Color(0.2f, 0f, 0.2f);

    private float detectionTargetAlpha;

    private void Start()
    {
        if (DetectionBar.Instance != null)
            DetectionBar.Instance.OnDetectionChanged += UpdateDetectionBar;

        if (MaskSystem.Instance != null)
            MaskSystem.Instance.OnCorruptionChanged += UpdateCorruptionBar;
    }

    private void OnDestroy()
    {
        if (DetectionBar.Instance != null)
            DetectionBar.Instance.OnDetectionChanged -= UpdateDetectionBar;

        if (MaskSystem.Instance != null)
            MaskSystem.Instance.OnCorruptionChanged -= UpdateCorruptionBar;
    }

    private void Update()
    {
        // Fade detection bar in/out
        if (detectionGroup != null)
        {
            detectionGroup.alpha = Mathf.Lerp(detectionGroup.alpha, detectionTargetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    private void UpdateDetectionBar(float normalized)
    {
        if (detectionFill != null)
        {
            detectionFill.fillAmount = normalized;
            detectionFill.color = Color.Lerp(detectionSafeColor, detectionDangerColor, normalized);
        }

        // Show bar when detection is above zero, hide when empty
        detectionTargetAlpha = normalized > 0.01f ? 1f : 0f;
    }

    private void UpdateCorruptionBar(float normalized)
    {
        if (corruptionFill != null)
        {
            corruptionFill.fillAmount = normalized;
            corruptionFill.color = Color.Lerp(corruptionLowColor, corruptionHighColor, normalized);
        }
    }
}