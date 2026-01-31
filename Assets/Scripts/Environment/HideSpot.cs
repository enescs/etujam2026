using UnityEngine;

public class HideSpot : MonoBehaviour
{
    [SerializeField] private int hiddenSortingOrder = 10; // Çalının önünde görünsün
    [SerializeField] private float interactionRange = 4f;
    [SerializeField] private float hiddenAlpha = 0.4f;

    private SpriteRenderer[] allRenderers;
    private BoxCollider2D bushCollider;
    private bool playerIsHiding;
    private Color[] originalColors;
    private Transform playerTransform;

    public bool PlayerIsHiding => playerIsHiding;
    public Bounds HideBounds => bushCollider != null ? bushCollider.bounds : new Bounds();

    public static HideSpot CurrentHideSpot { get; private set; }

    private void Awake()
    {
        // Tüm sprite renderer'ları al (kendisi + child'lar)
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        bushCollider = GetComponent<BoxCollider2D>();

        // Orijinal renkleri kaydet
        originalColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
        {
            originalColors[i] = allRenderers[i].color;
        }
    }

    private void Start()
    {
        // Player referansını al
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        // Collider'ı solid yap (trigger değil)
        if (bushCollider != null)
            bushCollider.isTrigger = false;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            // Player'ı tekrar bulmayı dene
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        // T tuşuyla gir/çık
        if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log($"[HideSpot] T pressed. Hiding: {playerIsHiding}, InRange: {IsPlayerInRange()}");

            if (playerIsHiding)
            {
                ExitHiding();
            }
            else if (IsPlayerInRange())
            {
                EnterHiding();
            }
            else
            {
                float dist = bushCollider != null
                    ? Vector2.Distance(bushCollider.ClosestPoint(playerTransform.position), playerTransform.position)
                    : Vector2.Distance(transform.position, playerTransform.position);
                Debug.Log($"[HideSpot] Not in range. Distance from edge: {dist:F2}, Required: {interactionRange}");
            }
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;

        // Collider varsa kenarından mesafe ölç, yoksa merkezden
        if (bushCollider != null)
        {
            Vector2 closestPoint = bushCollider.ClosestPoint(playerTransform.position);
            float distance = Vector2.Distance(closestPoint, playerTransform.position);
            return distance <= interactionRange;
        }
        else
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            return distance <= interactionRange;
        }
    }

    private void EnterHiding()
    {
        PlayerHiding playerHiding = PlayerHiding.Instance;
        if (playerHiding == null) return;

        playerIsHiding = true;
        CurrentHideSpot = this;

        // Oyuncuyla çalı arasındaki collision'ı kapat
        Collider2D playerCollider = playerTransform.GetComponent<Collider2D>();
        if (playerCollider != null && bushCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, bushCollider, true);
        }

        // Oyuncuyu çalının ortasına taşı
        playerHiding.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            playerHiding.transform.position.z
        );

        // Tüm sprite'ları saydam yap
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Color c = allRenderers[i].color;
            c.a = hiddenAlpha;
            allRenderers[i].color = c;
        }

        playerHiding.Hide(hiddenSortingOrder, this);
        Debug.Log($"[HideSpot] Player ENTERED bush - Alpha: {hiddenAlpha}, Player pos: {playerHiding.transform.position}, Bush pos: {transform.position}");
    }

    public void ExitHiding()
    {
        if (!playerIsHiding) return;

        PlayerHiding playerHiding = PlayerHiding.Instance;
        if (playerHiding != null)
        {
            // Oyuncuyu çalının kenarına çıkar
            Vector3 exitPos = transform.position;
            exitPos.y -= (bushCollider != null ? bushCollider.bounds.extents.y + 0.5f : 1f);
            exitPos.z = playerHiding.transform.position.z;
            playerHiding.transform.position = exitPos;

            playerHiding.Unhide();
        }

        // Collision'ı tekrar aç
        if (playerTransform != null)
        {
            Collider2D playerCollider = playerTransform.GetComponent<Collider2D>();
            if (playerCollider != null && bushCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, bushCollider, false);
            }
        }

        // Tüm sprite'ların rengini geri getir
        for (int i = 0; i < allRenderers.Length; i++)
        {
            allRenderers[i].color = originalColors[i];
        }

        playerIsHiding = false;
        CurrentHideSpot = null;
        Debug.Log("[HideSpot] Player EXITED bush");
    }

    private void OnDrawGizmosSelected()
    {
        // Etkileşim mesafesini göster
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
