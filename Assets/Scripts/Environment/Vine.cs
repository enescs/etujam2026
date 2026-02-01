using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sarmaşık - normalde geçilmez, X tuşuyla 1 saniyede kırılır.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Vine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float tearDuration = 1f;
    [SerializeField] private float interactionRange = 1.5f;

    [Header("Visual Feedback")]
    [SerializeField] private Color tearingColor = new Color(1f, 0.5f, 0.5f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Collider2D col;
    private Transform playerTransform;

    private bool isTearing;
    private float tearProgress;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Collider solid olmalı
        if (col != null)
            col.isTrigger = false;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        // X tuşuna basılı tutuluyorsa ve menzildeyse
        if (Keyboard.current.xKey.isPressed && IsPlayerInRange())
        {
            if (!isTearing)
            {
                isTearing = true;
                tearProgress = 0f;
                Debug.Log("[Vine] Started tearing");
            }

            tearProgress += Time.deltaTime;

            // Görsel feedback - renk değiştir
            if (spriteRenderer != null)
            {
                float t = tearProgress / tearDuration;
                spriteRenderer.color = Color.Lerp(originalColor, tearingColor, t);
            }

            // Tamamlandı mı?
            if (tearProgress >= tearDuration)
            {
                TearVine();
            }
        }
        else
        {
            // X bırakıldı veya menzil dışına çıkıldı
            if (isTearing)
            {
                isTearing = false;
                tearProgress = 0f;

                // Rengi geri getir
                if (spriteRenderer != null)
                    spriteRenderer.color = originalColor;

                Debug.Log("[Vine] Tearing cancelled");
            }
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;

        // Collider varsa en yakın noktadan mesafe ölç
        if (col != null)
        {
            Vector2 closestPoint = col.ClosestPoint(playerTransform.position);
            float distance = Vector2.Distance(closestPoint, playerTransform.position);
            return distance <= interactionRange;
        }
        else
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            return distance <= interactionRange;
        }
    }

    private void TearVine()
    {
        Debug.Log("[Vine] Vine torn!");

        // Sarmaşığı yok et veya deaktive et
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
