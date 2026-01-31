using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    public static PlayerHiding Instance { get; private set; }

    public bool IsHidden { get; private set; }

    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;
    private HideSpot currentHideSpot;
    private Rigidbody2D rb;

    private void Awake()
    {
        Instance = this;
        // Önce kendinde ara, yoksa child'larda ara
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
            Debug.Log($"[PlayerHiding] Found SpriteRenderer on {spriteRenderer.gameObject.name}, sorting order: {originalSortingOrder}");
        }
        else
        {
            Debug.LogError("[PlayerHiding] SpriteRenderer NOT FOUND on player or children!");
        }
    }

    private void FixedUpdate()
    {
        // Gizliyken çalının sınırları içinde kal
        if (IsHidden && currentHideSpot != null)
        {
            ClampToBounds();
        }
    }

    private void ClampToBounds()
    {
        Bounds bushBounds = currentHideSpot.HideBounds;
        Vector3 pos = transform.position;

        // Oyuncunun sprite boyutunu al
        float playerHalfWidth = 0f;
        float playerHalfHeight = 0f;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Sprite bounds'unu world scale ile çarp
            Vector3 scale = transform.lossyScale;
            playerHalfWidth = (spriteRenderer.sprite.bounds.extents.x * Mathf.Abs(scale.x));
            playerHalfHeight = (spriteRenderer.sprite.bounds.extents.y * Mathf.Abs(scale.y));
        }

        // Sınırları oyuncu boyutuna göre daralt
        float minX = bushBounds.min.x + playerHalfWidth;
        float maxX = bushBounds.max.x - playerHalfWidth;
        float minY = bushBounds.min.y + playerHalfHeight;
        float maxY = bushBounds.max.y - playerHalfHeight;

        // Eğer çalı çok küçükse, ortada kal
        if (minX > maxX) minX = maxX = bushBounds.center.x;
        if (minY > maxY) minY = maxY = bushBounds.center.y;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }

    public void Hide(int sortingOrder, HideSpot hideSpot)
    {
        IsHidden = true;
        currentHideSpot = hideSpot;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
            Debug.Log($"[PlayerHiding] Player HIDDEN - Sorting order set to {sortingOrder}, position: {transform.position}");
        }
        else
        {
            Debug.LogError("[PlayerHiding] Cannot set sorting order - SpriteRenderer is null!");
        }
    }

    public void Unhide()
    {
        IsHidden = false;
        currentHideSpot = null;

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = originalSortingOrder;

        Debug.Log("[PlayerHiding] Player is now VISIBLE");
    }
}
