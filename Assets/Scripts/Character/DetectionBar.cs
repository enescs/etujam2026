using UnityEngine;
using System;

public class DetectionBar : MonoBehaviour
{
    public static DetectionBar Instance { get; private set; }

    [Header("Detection Settings")]
    [SerializeField] private float maxDetection = 100f;
    [SerializeField] private float drainRate = 15f; // per second when no enemy has LOS

    public float CurrentDetection { get; private set; }
    public float DetectionNormalized => CurrentDetection / maxDetection;
    public bool IsDetected => CurrentDetection >= maxDetection;

    /// <summary>
    /// Fired when the bar fills completely. The EnemyAI that pushed it over is passed.
    /// </summary>
    public event Action<EnemyAI> OnFullDetection;

    /// <summary>
    /// Fired every frame with the normalized value (0-1) for UI updates.
    /// </summary>
    public event Action<float> OnDetectionChanged;

    private bool anyEnemyHasLOS;
    private bool detectionTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void FixedUpdate()
    {
        // If no enemy added detection this physics frame, drain
        if (!anyEnemyHasLOS && CurrentDetection > 0f)
        {
            CurrentDetection -= drainRate * Time.fixedDeltaTime;
            CurrentDetection = Mathf.Max(CurrentDetection, 0f);
        }

        // Reset for next physics frame
        anyEnemyHasLOS = false;

        // Reset trigger flag when bar empties fully
        if (CurrentDetection <= 0f)
            detectionTriggered = false;
    }

    private void LateUpdate()
    {
        // UI update only
        OnDetectionChanged?.Invoke(DetectionNormalized);
    }

    /// <summary>
    /// Called by individual EnemyAI each frame they have LOS.
    /// </summary>
    public void AddDetection(float amount, EnemyAI source)
    {
        anyEnemyHasLOS = true;
        CurrentDetection += amount;
        CurrentDetection = Mathf.Min(CurrentDetection, maxDetection);

        if (CurrentDetection >= maxDetection && !detectionTriggered)
        {
            detectionTriggered = true;
            OnFullDetection?.Invoke(source);
        }
    }

    /// <summary>
    /// Call when mask is put on to reset detection state.
    /// </summary>
    public void ResetDetection()
    {
        CurrentDetection = 0f;
        detectionTriggered = false;
    }
}