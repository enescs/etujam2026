using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the player alongside existing movement scripts.
/// Handles climbing when overlapping a ClimbableRope.
/// Uses direct Physics2D overlap instead of trigger callbacks.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClimb : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask ropeLayer;
    [SerializeField] private float climbSpeed=5;

    [SerializeField] private float ropeDetectRadius = 0.5f;

    public bool IsClimbing { get; private set; }
    public ClimbableRope CurrentRope { get; private set; }

    private Rigidbody2D rb;
    private bool wasKinematic;
    private float originalGravityScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (!IsClimbing)
        {
            // Check for nearby rope when pressing W or S
            if (kb.wKey.isPressed || kb.sKey.isPressed)
            {
                ClimbableRope rope = FindNearbyRope();
                if (rope != null)
                {
                    AttachToRope(rope);
                }
            }
            return;
        }

        // Currently climbing
        if (rb == null)
        {
            Debug.LogError("[PlayerClimb] No Rigidbody2D found on player! Detaching from rope.");
            DetachFromRope();
            return;
        }

        float vertical = 0f;
        if (kb.wKey.isPressed) vertical += climbSpeed;
        if (kb.sKey.isPressed) vertical -= climbSpeed;

        // Move along rope
        Vector2 pos = rb.position;
        pos.y += vertical * CurrentRope.ClimbSpeed * Time.deltaTime;

        // Clamp to rope bounds
        pos.y = Mathf.Clamp(pos.y, CurrentRope.BottomY, CurrentRope.TopY);

        // Lock X to rope center
        pos.x = CurrentRope.RopeXPosition;

        rb.MovePosition(pos);

        // Dismount at top - check if trying to climb higher than top
        if (pos.y >= CurrentRope.TopY && vertical > 0f)
        {
            DetachFromRope();
            return;
        }
        // Dismount at bottom - check if trying to climb lower than bottom
        else if (pos.y <= CurrentRope.BottomY && vertical < 0f)
        {
            DetachFromRope();
            return;
        }
    }

    private ClimbableRope FindNearbyRope()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, ropeDetectRadius, ropeLayer);
        if (hit != null)
        {
            return hit.GetComponent<ClimbableRope>();
        }
        return null;
    }

    private void AttachToRope(ClimbableRope rope)
    {
        IsClimbing = true;
        CurrentRope = rope;

        // Store original Rigidbody2D state
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            originalGravityScale = rb.gravityScale;

            // Make kinematic to prevent physics interference
            rb.isKinematic = true;
            rb.linearVelocity = Vector2.zero;
        }

        // Snap to rope X
        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        pos.x = rope.RopeXPosition;

        if (rb != null)
            rb.MovePosition(pos);
        else
            transform.position = pos;

        Debug.Log($"[PlayerClimb] Attached to rope: {rope.name}");
    }

    private void DetachFromRope()
    {
        Debug.Log($"[PlayerClimb] Detached from rope");

        // Restore original Rigidbody2D state
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.gravityScale = originalGravityScale;
        }

        IsClimbing = false;
        CurrentRope = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, ropeDetectRadius);
    }
}