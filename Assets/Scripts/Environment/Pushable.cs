using UnityEngine;

public class Pushable : MonoBehaviour
{
    [SerializeField] private float dragSmoothness = 10f;
    [SerializeField] private bool canBePushedInRealWorld = true;
    [SerializeField] private bool canBePushedInSpiritWorld = true;
    [SerializeField] private bool isEnemy = false;

    private Rigidbody2D rb;
    private bool isBeingPushed;
    private Transform pusher;
    private Vector2 offsetFromPusher;

    public bool IsBeingPushed => isBeingPushed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public bool CanBePushed()
    {
        // Mask durumuna göre kontrol
        bool inSpiritWorld = MaskSystem.Instance != null && MaskSystem.Instance.IsMaskOn;

        // Düşmanlar sadece spirit world'de itilebilir
        if (isEnemy)
        {
            return inSpiritWorld;
        }

        if (inSpiritWorld)
            return canBePushedInSpiritWorld;
        else
            return canBePushedInRealWorld;
    }

    /// <summary>
    /// Düşman olarak ayarla (sadece spirit world'de itilebilir)
    /// </summary>
    public void SetAsEnemy()
    {
        isEnemy = true;
        canBePushedInRealWorld = false;
        canBePushedInSpiritWorld = true;
    }

    public void StartPush(Transform pusherTransform)
    {
        if (!CanBePushed()) return;

        isBeingPushed = true;
        pusher = pusherTransform;
        offsetFromPusher = (Vector2)transform.position - (Vector2)pusher.position;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // İterken çarpışmayı kapat
        Collider2D pusherCollider = pusher.GetComponent<Collider2D>();
        Collider2D myCollider = GetComponent<Collider2D>();
        if (pusherCollider != null && myCollider != null)
        {
            Physics2D.IgnoreCollision(pusherCollider, myCollider, true);
        }

        Debug.Log($"[Pushable] Started pushing {gameObject.name}");
    }

    public void StopPush()
    {
        if (!isBeingPushed) return;

        // Çarpışmayı tekrar aç
        if (pusher != null)
        {
            Collider2D pusherCollider = pusher.GetComponent<Collider2D>();
            Collider2D myCollider = GetComponent<Collider2D>();
            if (pusherCollider != null && myCollider != null)
            {
                Physics2D.IgnoreCollision(pusherCollider, myCollider, false);
            }
        }

        isBeingPushed = false;
        pusher = null;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log($"[Pushable] Stopped pushing {gameObject.name}");
    }

    private void FixedUpdate()
    {
        if (!isBeingPushed || pusher == null) return;

        // Pusher'ı takip et (offset'i koruyarak)
        Vector2 targetPos = (Vector2)pusher.position + offsetFromPusher;

        if (rb != null)
        {
            Vector2 newPos = Vector2.Lerp(rb.position, targetPos, dragSmoothness * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
        else
        {
            transform.position = Vector2.Lerp(transform.position, targetPos, dragSmoothness * Time.fixedDeltaTime);
        }
    }

    private void OnDestroy()
    {
        // Temizlik
        if (isBeingPushed)
            StopPush();
    }
}
