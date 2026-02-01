using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ekranın kenarında mask süresini gösteren UI.
/// Mask takılı değilken gizlenir, takılıyken süre azaldıkça bar dolar.
/// </summary>
public class MaskDurationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillBar;
    [SerializeField] private Image backgroundBar;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Colors")]
    [SerializeField] private Color safeColor = Color.cyan;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningThreshold = 0.5f; // %50'nin altında sarı
    [SerializeField] private float dangerThreshold = 0.25f; // %25'in altında kırmızı

    [Header("Auto Create UI")]
    [SerializeField] private bool autoCreateUI = true;

    private Canvas canvas;
    private GameObject uiContainer;
    private bool wasShowingLastFrame;

    private void Start()
    {
        if (autoCreateUI && fillBar == null)
        {
            CreateUI();
        }

        // Başlangıçta gizle
        if (uiContainer != null)
            uiContainer.SetActive(false);
    }

    private void Update()
    {
        // MaskSystem yoksa bekle
        if (MaskSystem.Instance == null) return;

        bool shouldShow = MaskSystem.Instance.IsMaskOn;

        // Göster/Gizle
        if (shouldShow != wasShowingLastFrame)
        {
            if (uiContainer != null)
                uiContainer.SetActive(shouldShow);
            wasShowingLastFrame = shouldShow;
        }

        // Bar güncelle (mask açıkken)
        if (shouldShow)
        {
            float normalized = MaskSystem.Instance.MaskDurationNormalized;
            UpdateBar(normalized);
        }
    }

    private void CreateUI()
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("MaskDurationCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Container (sağ üst köşe) - BÜYÜK BAR
        uiContainer = new GameObject("MaskDurationContainer");
        uiContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(1f, 1f);
        containerRect.anchoredPosition = new Vector2(-30f, -30f);
        containerRect.sizeDelta = new Vector2(350f, 60f);

        // Background bar
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(uiContainer.transform, false);
        backgroundBar = bgObj.AddComponent<Image>();
        backgroundBar.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill bar
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(uiContainer.transform, false);
        fillBar = fillObj.AddComponent<Image>();
        fillBar.color = safeColor;
        fillBar.type = Image.Type.Filled;
        fillBar.fillMethod = Image.FillMethod.Horizontal;
        fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillBar.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        // Timer text - BÜYÜK FONT
        GameObject textObj = new GameObject("TimerText");
        textObj.transform.SetParent(uiContainer.transform, false);
        timerText = textObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "10.0s";
        timerText.fontSize = 28f;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyles.Bold;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Mask icon/label (üstte)
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(uiContainer.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "MASK";
        label.fontSize = 16f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0.8f);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 5f);
        labelRect.sizeDelta = new Vector2(0f, 20f);
    }

    private void ShowUI()
    {
        if (uiContainer != null)
            uiContainer.SetActive(true);
    }

    private void HideUI()
    {
        if (uiContainer != null)
            uiContainer.SetActive(false);
    }

    private void UpdateBar(float normalized)
    {
        if (fillBar != null)
        {
            fillBar.fillAmount = normalized;

            // Renk güncelle
            if (normalized <= dangerThreshold)
                fillBar.color = dangerColor;
            else if (normalized <= warningThreshold)
                fillBar.color = warningColor;
            else
                fillBar.color = safeColor;
        }

        // Timer text güncelle
        if (timerText != null && MaskSystem.Instance != null)
        {
            float remaining = MaskSystem.Instance.RemainingMaskDuration;
            timerText.text = $"{remaining:F1}s";

            // Süre bittiyse kırmızı yap
            if (remaining <= 0f)
            {
                timerText.color = dangerColor;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
}
