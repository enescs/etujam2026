using UnityEngine;

public class ThrowableLure : MonoBehaviour
{
    [SerializeField] private float lureRadius = 10f;
    [SerializeField] private float pulseInterval = 0.5f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private bool activated;
    private float pulseTimer;
    private float lifetimeTimer;

    /// <summary>
    /// Called by PlayerInteraction when the item lands after being thrown.
    /// </summary>
    public void Activate()
    {
        activated = true;
        pulseTimer = 0f;
        lifetimeTimer = lifetime;
    }

    /// <summary>
    /// Called by PlayerInteraction when the item is picked up again.
    /// </summary>
    public void ResetLure()
    {
        activated = false;
    }

    private void Update()
    {
        if (!activated) return;

        // Countdown to destruction
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Only lure while mask is on
        if (MaskSystem.Instance == null || !MaskSystem.Instance.IsMaskOn)
            return;

        pulseTimer -= Time.deltaTime;
        if (pulseTimer <= 0f)
        {
            pulseTimer = pulseInterval;
            PullNearbyEnemies();
        }
    }

    private void PullNearbyEnemies()
    {
        // DEBUG: Tüm collider'ları bul (layer farketmez)
        Collider2D[] allHits = Physics2D.OverlapCircleAll(transform.position, lureRadius);
        Debug.Log($"[ThrowableLure] DEBUG - ALL colliders in range: {allHits.Length}");
        foreach (var c in allHits)
        {
            Debug.Log($"  -> {c.gameObject.name} (Layer: {LayerMask.LayerToName(c.gameObject.layer)})");
        }
        
        // Asıl arama (sadece enemyLayer)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, lureRadius, enemyLayer);
        Debug.Log($"[ThrowableLure] Pulse! Found {hits.Length} colliders in enemyLayer. Mask on: {MaskSystem.Instance?.IsMaskOn}");
        
        foreach (Collider2D col in hits)
        {
            // Kendini hariç tut
            if (col.gameObject == gameObject) continue;
            
            // Parent'ta da ara - collider child'da, script parent'ta olabilir
            EnemyAI enemy = col.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                Debug.Log($"[ThrowableLure] Luring enemy: {enemy.name}, state: {enemy.CurrentState}");
                enemy.LureToPosition(transform.position);
            }
            else
            {
                Debug.LogWarning($"[ThrowableLure] Collider {col.gameObject.name} has no EnemyAI in parent!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, lureRadius);
    }
}