using UnityEngine;

/// <summary>
/// Delik - Cliff gibi ama pushable taşla kapatılabilir.
/// Taş üstündeyken güvenli, yoksa içine düşülür.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hole : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveToCenterDuration = 0.3f;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float shrinkScale = 0.1f;

    [Header("Cover Detection")]
    [SerializeField] private string pushableTag = "pushable"; // Pushable taşların tag'ı

    private Collider2D col;
    private bool isCovered;
    private bool isPermanentlyCovered; // Taş yerleştikten sonra kalıcı olarak kapalı
    private GameObject coveringObject;

    // Player deliğe düşüyor mu?
    public static bool IsPlayerFallingInHole { get; private set; }

    public bool IsCovered => isCovered;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Update()
    {
        // Delik üstünde taş var mı kontrol et
        CheckForCover();
    }

    private void CheckForCover()
    {
        // Kalıcı olarak kapatıldıysa kontrol etmeye gerek yok
        if (isPermanentlyCovered)
        {
            isCovered = true;
            return;
        }

        // Deliğin merkezinde pushable obje var mı? (tüm collider'ları tara)
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            col.bounds.center,
            col.bounds.size * 0.8f, // Biraz küçült ki tam üstünde olsun
            0f
        );

        bool wasCovered = isCovered;
        isCovered = false;
        coveringObject = null;

        foreach (var hit in hits)
        {
            // Tag ile kontrol et
            if (!hit.CompareTag(pushableTag)) continue;

            Pushable pushable = hit.GetComponent<Pushable>();
            if (pushable != null && !pushable.IsBeingPushed)
            {
                // Taş deliğin üstünde ve itilmiyor
                isCovered = true;
                coveringObject = hit.gameObject;
                break;
            }
        }

        // Debug
        if (wasCovered != isCovered)
        {
            Debug.Log($"[Hole] {gameObject.name} is now {(isCovered ? "COVERED" : "OPEN")}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Delik kapalıysa kimse düşmez
        if (isCovered)
        {
            Debug.Log($"[Hole] {other.name} stepped on covered hole - safe!");
            return;
        }

        // Pushable obje (taş) deliğe girdi - üstüne yerleş, düşürme
        if (other.CompareTag(pushableTag))
        {
            Pushable pushable = other.GetComponent<Pushable>();
            if (pushable == null) return;

            // Taş itiliyorsa üstüne yerleşsin, değilse bir şey yapma
            if (pushable.IsBeingPushed)
            {
                Debug.Log($"[Hole] Pushable {other.name} placed on hole!");

                // Önce push'ı durdur - böylece taş artık "covering" sayılır
                pushable.StopPush();

                // Hemen covered olarak işaretle (snap animasyonu bitmeden güvenli olsun)
                isCovered = true;
                coveringObject = other.gameObject;

                // Taş deliğin tam ortasına yerleşecek (snap)
                StartCoroutine(SnapToCenter(other.gameObject));
            }
            return;
        }

        // Player kontrolü
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Hole] Player fell into the hole!");
            StartCoroutine(FallAnimation(other.gameObject, true));
            return;
        }

        // Enemy kontrolü
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            Debug.Log($"[Hole] Enemy {other.gameObject.name} fell into the hole!");
            StartCoroutine(FallAnimation(other.gameObject, false));
        }
    }

    private System.Collections.IEnumerator SnapToCenter(GameObject obj)
    {
        // Taşı deliğin ortasına yerleştir
        Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, obj.transform.position.z);

        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        obj.transform.position = targetPos;

        // Taşın collider'ını kapat - üstünden geçilebilsin
        Collider2D stoneCollider = obj.GetComponent<Collider2D>();
        if (stoneCollider != null)
        {
            stoneCollider.enabled = false;
        }

        // Kalıcı olarak kapalı işaretle
        isPermanentlyCovered = true;
        isCovered = true;

        Debug.Log($"[Hole] {obj.name} snapped to hole center, hole permanently covered");
    }

    private System.Collections.IEnumerator FallAnimation(GameObject obj, bool isPlayer)
    {
        // Hareketi durdur
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Player ise input'u kapat
        if (isPlayer)
        {
            IsPlayerFallingInHole = true;

            PlayerController pc = obj.GetComponent<PlayerController>();
            if (pc != null)
                pc.enabled = false;
        }
        else
        {
            // Enemy ise AI'ı durdur
            EnemyAI ai = obj.GetComponent<EnemyAI>();
            if (ai != null)
                ai.enabled = false;
        }

        // 1. AŞAMA: Deliğin ortasına git
        Vector3 startPos = obj.transform.position;
        Vector3 centerPos = new Vector3(transform.position.x, transform.position.y, startPos.z);
        float elapsed = 0f;

        while (elapsed < moveToCenterDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToCenterDuration;
            float easedT = t * t * (3f - 2f * t);
            obj.transform.position = Vector3.Lerp(startPos, centerPos, easedT);
            yield return null;
        }

        obj.transform.position = centerPos;

        // 2. AŞAMA: Küçülerek düş
        Vector3 originalScale = obj.transform.localScale;
        Vector3 targetScale = originalScale * shrinkScale;
        elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            float easedT = t * t;
            obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, easedT);
            yield return null;
        }

        // Sonuç
        if (isPlayer)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
        else
        {
            Destroy(obj);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isCovered ? new Color(0f, 1f, 0f, 0.3f) : new Color(0.5f, 0f, 0.5f, 0.3f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one);
        }
    }
}
