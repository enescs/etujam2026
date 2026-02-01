using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Camera Bounds")]
    [Tooltip("Haritanın sınırlarını belirleyen PolygonCollider2D - Is Trigger olmalı!")]
    [SerializeField] private PolygonCollider2D boundsCollider;

    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private Bounds cachedBounds;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // Bounds'u cache'le - her frame hesaplama
        if (boundsCollider != null)
        {
            cachedBounds = boundsCollider.bounds;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // Önce bounds uygula (AABB kullanarak), sonra smooth damp
        if (boundsCollider != null && cam != null)
        {
            desiredPosition = ClampToBounds(desiredPosition);
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.position = smoothedPosition;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // PolygonCollider'ın AABB (axis-aligned bounding box) sınırlarını kullan
        // Bu tutarlı sonuç verir ve titreme olmaz
        float minX = cachedBounds.min.x + halfWidth;
        float maxX = cachedBounds.max.x - halfWidth;
        float minY = cachedBounds.min.y + halfHeight;
        float maxY = cachedBounds.max.y - halfHeight;

        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        float clampedY = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(clampedX, clampedY, position.z);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetBoundsCollider(PolygonCollider2D collider)
    {
        boundsCollider = collider;
        if (collider != null)
            cachedBounds = collider.bounds;
    }
}
