using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask throwableLayer;
    [SerializeField] private Transform holdPoint;

    [Header("Throw Settings")]
    [SerializeField] private float throwDuration = 0.6f;
    [SerializeField] private float arcHeight = 0.5f;

    [Header("Push/Pull Settings")]
    [SerializeField] private float pushRange = 1.5f;
    [SerializeField] private LayerMask pushableLayer;

    private GameObject heldItem;
    private Camera mainCamera;
    private Pushable currentPushable;

    // Push durumunu dışarıya açıkla (PlayerController hız için kullanır)
    public bool IsPushing => currentPushable != null;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // E tuşu - eşya al/bırak
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldItem == null)
                TryPickup();
            else
                DropItem();
        }

        // Sol tık - fırlat
        if (Mouse.current.leftButton.wasPressedThisFrame && heldItem != null)
        {
            ThrowItem();
        }

        // R tuşu - maske tak/çıkar
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ToggleMask();
        }

        // Q tuşu - itme/çekme (toggle: bir bas başla, bir daha bas bırak)
        if (Keyboard.current.qKey.wasPressedThisFrame && heldItem == null)
        {
            if (currentPushable == null)
                TryStartPush();
            else
                StopPush();
        }
    }

    private void ToggleMask()
    {
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.ToggleMask();
        }
    }

    private void TryPickup()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRadius, throwableLayer);

        if (colliders.Length == 0) return;

        // En yakın eşyayı bul
        Collider2D closest = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = col;
            }
        }

        if (closest != null)
        {
            heldItem = closest.gameObject;

            // Rigidbody2D'yi devre dışı bırak
            Rigidbody2D itemRb = heldItem.GetComponent<Rigidbody2D>();
            if (itemRb != null)
            {
                itemRb.linearVelocity = Vector2.zero;
                itemRb.bodyType = RigidbodyType2D.Kinematic;
            }

            // Eşyayı holdPoint'e bağla
            heldItem.transform.SetParent(holdPoint != null ? holdPoint : transform);
            heldItem.transform.localPosition = Vector3.zero;
        }
    }

    private void DropItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);

        // Z pozisyonunu ayarla (önde görünsün)
        Vector3 pos = heldItem.transform.position;
        pos.z = -1f;
        heldItem.transform.position = pos;

        Rigidbody2D itemRb = heldItem.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
        }

        heldItem = null;
    }

    private void ThrowItem()
    {
        if (heldItem == null) return;

        // Mouse pozisyonunu world space'e çevir
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        targetPos.z = -1f;

        // Eşyayı bırak
        GameObject thrownItem = heldItem;
        heldItem.transform.SetParent(null);
        heldItem = null;

        // Rigidbody'yi kapat, coroutine ile hareket ettir
        Rigidbody2D itemRb = thrownItem.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Kinematic;
            itemRb.linearVelocity = Vector2.zero;
        }

        StartCoroutine(MoveToTarget(thrownItem, targetPos));
    }

    private IEnumerator MoveToTarget(GameObject item, Vector3 targetPos)
    {
        Vector3 startPos = item.transform.position;
        startPos.z = -1f; // Z'yi -1 yap (kameraya yakın)
        float elapsed = 0f;

        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / throwDuration);

            // Smooth Step: yavaş başla, ortada hızlan, yavaş bitir
            float easedT = t * t * (3f - 2f * t);

            // XY pozisyonu (düz lerp)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedT);

            // Yay efekti (ortada yukarı, başta/sonda aşağı)
            float arc = 4f * arcHeight * t * (1f - t);
            currentPos.y += arc;
            currentPos.z = -1f; // Z her zaman -1 (önde görünsün)

            item.transform.position = currentPos;
            yield return null;
        }

        // Tam hedefe yerleştir
        targetPos.z = -1f;
        item.transform.position = targetPos;

        // Rigidbody'yi tekrar aç (durağan kalacak)
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.linearVelocity = Vector2.zero;
        }
    }

    private void TryStartPush()
    {
        Debug.Log($"[Push] TryStartPush called. pushRange={pushRange}, pushableLayer={pushableLayer.value}");
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pushRange, pushableLayer);

        Debug.Log($"[Push] Found {colliders.Length} colliders in range");
        
        if (colliders.Length == 0) return;

        // En yakın pushable'ı bul
        Collider2D closest = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            Debug.Log($"[Push] Checking collider: {col.gameObject.name}");
            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = col;
            }
        }

        if (closest != null)
        {
            Pushable pushable = closest.GetComponent<Pushable>();
            Debug.Log($"[Push] Closest: {closest.gameObject.name}, has Pushable: {pushable != null}");
            
            if (pushable != null)
            {
                Debug.Log($"[Push] CanBePushed: {pushable.CanBePushed()}");
                if (pushable.CanBePushed())
                {
                    currentPushable = pushable;
                    currentPushable.StartPush(transform);
                    Debug.Log($"[Push] Started pushing {closest.gameObject.name}");
                }
            }
        }
    }

    private void StopPush()
    {
        if (currentPushable != null)
        {
            currentPushable.StopPush();
            currentPushable = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pushRange);
    }
}
