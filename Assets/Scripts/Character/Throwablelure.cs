using UnityEngine;

public class ThrowableLure : MonoBehaviour
{
    [SerializeField] private float lureRadius = 10f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private LayerMask enemyLayer;

    private bool activated;

    /// <summary>
    /// Call this after the object lands at its thrown position.
    /// </summary>
    public void Activate()
    {
        if (activated) return;
        activated = true;

        // Only works in spirit world
        if (MaskSystem.Instance == null || !MaskSystem.Instance.IsMaskOn)
        {
            Destroy(gameObject);
            return;
        }

        // Find all enemies in radius and lure them
        Collider[] hits = Physics.OverlapSphere(transform.position, lureRadius, enemyLayer);
        foreach (Collider col in hits)
        {
            EnemyAI enemy = col.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.LureToPosition(transform.position);
            }
        }

        Destroy(gameObject, lifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, lureRadius);
    }
}