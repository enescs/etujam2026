using UnityEngine;

public class DestructibleObstacle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float destroyTime = 2f;

    private float currentProgress = 0f;
    private bool isBeingDestroyed = false;

    public float Progress => currentProgress / destroyTime;
    public bool IsBeingDestroyed => isBeingDestroyed;

    public void StartDestroying()
    {
        isBeingDestroyed = true;
    }

    public void StopDestroying()
    {
        isBeingDestroyed = false;
        currentProgress = 0f;
    }

    void Update()
    {
        if (isBeingDestroyed)
        {
            currentProgress += Time.deltaTime;
            if (currentProgress >= destroyTime)
            {
                Destroy(gameObject);
            }
        }
    }
}
