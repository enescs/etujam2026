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

    private GameObject heldItem;
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // E key - pick up / drop
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldItem == null)
                TryPickup();
            else
                DropItem();
        }

        // Left click - throw (only in spirit world)
        if (Mouse.current.leftButton.wasPressedThisFrame && heldItem != null
            && MaskSystem.Instance != null && MaskSystem.Instance.IsMaskOn)
        {
            ThrowItem();
        }

        // R key - mask toggle
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ToggleMask();
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

        // Find closest item
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

            Rigidbody2D itemRb = heldItem.GetComponent<Rigidbody2D>();
            if (itemRb != null)
            {
                itemRb.linearVelocity = Vector2.zero;
                itemRb.bodyType = RigidbodyType2D.Kinematic;
            }

            // Attach to hold point
            heldItem.transform.SetParent(holdPoint != null ? holdPoint : transform);
            heldItem.transform.localPosition = Vector3.zero;

            // Reset lure state so it can be thrown again
            ThrowableLure lure = heldItem.GetComponent<ThrowableLure>();
            if (lure != null)
            {
                lure.ResetLure();
            }
        }
    }

    private void DropItem()
    {
        if (heldItem == null) return;

        heldItem.transform.SetParent(null);

        // Stay kinematic so nothing can push it
        Rigidbody2D itemRb = heldItem.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Kinematic;
            itemRb.linearVelocity = Vector2.zero;
        }

        heldItem = null;
    }

    private void ThrowItem()
    {
        if (heldItem == null) return;

        // Convert mouse position to world space
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        targetPos.z = 0f;

        // Release item
        GameObject thrownItem = heldItem;
        heldItem.transform.SetParent(null);
        heldItem = null;

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
        float elapsed = 0f;

        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / throwDuration);

            float easedT = t * t * (3f - 2f * t);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedT);

            float arc = 4f * arcHeight * t * (1f - t);
            currentPos.y += arc;

            item.transform.position = currentPos;
            yield return null;
        }

        // Land at target
        item.transform.position = targetPos;

        // Remove physics so nothing can push it
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
            Destroy(itemRb);

        // Set collider to trigger so pickup still works but no physical interaction
        Collider2D itemCol = item.GetComponent<Collider2D>();
        if (itemCol != null)
            itemCol.isTrigger = true;

        // Activate lure on landing
        ThrowableLure lure = item.GetComponent<ThrowableLure>();
        if (lure != null)
        {
            lure.Activate();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}