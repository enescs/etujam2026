using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to each enemy alongside EnemyAI.
/// Creates a world-space detection bar above the enemy's head.
/// The bar shows the shared DetectionBar value and only appears when this enemy has LOS.
/// </summary>
public class EnemyDetectionBarUI : MonoBehaviour
{
    [Header("Bar Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1f, 0.15f);
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color fillSafeColor = Color.yellow;
    [SerializeField] private Color fillDangerColor = Color.red;

    private Canvas canvas;
    private Image backgroundImage;
    private Image fillImage;
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        CreateBar();
    }

    private void CreateBar()
    {
        // Create canvas on a child object
        GameObject canvasObj = new GameObject("DetectionBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        canvasObj.transform.localScale = Vector3.one;

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = barSize;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fillSafeColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Start hidden
        canvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (DetectionBar.Instance == null || enemyAI == null) return;

        float normalized = DetectionBar.Instance.DetectionNormalized;
        bool shouldShow = enemyAI.HasLOS && normalized > 0.01f;

        canvas.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            fillImage.fillAmount = normalized;
            fillImage.color = Color.Lerp(fillSafeColor, fillDangerColor, normalized);

            // Keep bar upright and not flipped with the sprite
            Vector3 scale = canvas.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (transform.localScale.x >= 0f ? 1f : -1f);
            canvas.transform.localScale = scale;
        }
    }
}