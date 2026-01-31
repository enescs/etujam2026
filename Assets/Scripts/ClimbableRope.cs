using UnityEngine;

/// <summary>
/// Attach to a rope GameObject with a Collider2D set as Trigger.
/// The collider defines the climbable area - works with any size.
/// </summary>
public class ClimbableRope : MonoBehaviour
{
    [Header("Climb Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float horizontalLockThreshold = 0.2f;

    /// <summary>
    /// How fast the player climbs on this rope. Accessed by PlayerClimb.
    /// </summary>
    public float ClimbSpeed => climbSpeed;

    /// <summary>
    /// The X position the player snaps to while climbing.
    /// </summary>
    public float RopeXPosition => transform.position.x;

    /// <summary>
    /// Top of the climbable area (from collider bounds).
    /// </summary>
    public float TopY
    {
        get
        {
            Collider2D col = GetComponent<Collider2D>();
            return col != null ? col.bounds.max.y : transform.position.y + 1f;
        }
    }

    /// <summary>
    /// Bottom of the climbable area (from collider bounds).
    /// </summary>
    public float BottomY
    {
        get
        {
            Collider2D col = GetComponent<Collider2D>();
            return col != null ? col.bounds.min.y : transform.position.y - 1f;
        }
    }
}