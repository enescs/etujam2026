using UnityEngine;

/// <summary>
/// Uçurum/çukur - üstüne basan düşer.
/// Player düşerse game over, Enemy düşerse ölür.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Cliff : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveToCenterDuration = 0.3f;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float shrinkScale = 0.1f;

    // Player cliff'e düşüyor mu? (Enemy'ler göremez)
    public static bool IsPlayerFalling { get; private set; }

    private void Awake()
    {
        // Collider trigger olmalı
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player kontrolü
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Cliff] Player fell into the cliff!");
            StartCoroutine(FallAnimation(other.gameObject, true));
            return;
        }

        // Enemy kontrolü
        EnemyAI enemy = other.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            Debug.Log($"[Cliff] Enemy {other.gameObject.name} fell into the cliff!");
            StartCoroutine(FallAnimation(other.gameObject, false));
        }
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

        // Player ise input'u kapat ve görünmez yap
        if (isPlayer)
        {
            IsPlayerFalling = true; // Enemy'ler artık göremez

            PlayerController pc = obj.GetComponent<PlayerController>();
            if (pc != null)
                pc.enabled = false;
        }

        // 1. AŞAMA: Önce uçurumun ortasına git
        Vector3 startPos = obj.transform.position;
        Vector3 centerPos = new Vector3(transform.position.x, transform.position.y, startPos.z);
        float elapsed = 0f;

        while (elapsed < moveToCenterDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveToCenterDuration;
            float easedT = t * t * (3f - 2f * t); // Smooth
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

            // Ease in - hızlanarak küçül
            float easedT = t * t;
            obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, easedT);

            yield return null;
        }

        // Sonuç
        if (isPlayer)
        {
            // Game over
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
        else
        {
            // Enemy'yi yok et
            Destroy(obj);
        }
    }

    private void OnDrawGizmos()
    {
        // Editor'da uçurumu göster
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

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
