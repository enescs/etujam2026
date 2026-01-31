using UnityEngine;

/// <summary>
/// Attach this to a tree or any object that should spawn lures.
/// Spawns a lure at dropPoint. When that lure is destroyed, spawns a new one after a delay.
/// </summary>
public class LureSpawner : MonoBehaviour
{
    [SerializeField] private GameObject lurePrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float respawnDelay = 3f;

    private GameObject currentLure;
    private float respawnTimer;
    private bool waitingToRespawn;

    private void Start()
    {
        SpawnLure();
    }

    private void Update()
    {
        // Check if current lure was destroyed
        if (currentLure == null && !waitingToRespawn)
        {
            waitingToRespawn = true;
            respawnTimer = respawnDelay;
        }

        if (waitingToRespawn)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                SpawnLure();
                waitingToRespawn = false;
            }
        }
    }

    private void SpawnLure()
    {
        Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position;
        currentLure = Instantiate(lurePrefab, spawnPos, Quaternion.identity);
    }
}