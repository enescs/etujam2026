using UnityEngine;
using System;

public class MaskSystem : MonoBehaviour
{
    public static MaskSystem Instance { get; private set; }

    [Header("Corruption Settings")]
    [SerializeField] private float maxCorruption = 100f;
    [SerializeField] private float corruptionPerSecond = 5f; // while mask is on
    [SerializeField] private float corruptionThresholdGameOver = 100f;

    [Header("Mask Toggle")]
    [SerializeField] private KeyCode maskKey = KeyCode.E;

    public bool IsMaskOn { get; private set; }
    public float CurrentCorruption { get; private set; }
    public float CorruptionNormalized => CurrentCorruption / maxCorruption;
    public bool IsFullyCorrupted => CurrentCorruption >= corruptionThresholdGameOver;

    /// <summary>
    /// Fired when mask is put on (entering spirit world).
    /// </summary>
    public event Action OnMaskOn;

    /// <summary>
    /// Fired when mask is taken off (returning to real world).
    /// </summary>
    public event Action OnMaskOff;

    /// <summary>
    /// Fired each frame with normalized corruption value for UI.
    /// </summary>
    public event Action<float> OnCorruptionChanged;

    /// <summary>
    /// Fired when corruption reaches the threshold.
    /// </summary>
    public event Action OnFullCorruption;

    private bool gameOverTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Instant toggle - no cooldown
        if (Input.GetKeyDown(maskKey) && !IsFullyCorrupted)
        {
            ToggleMask();
        }

        // Accumulate corruption while mask is on
        if (IsMaskOn)
        {
            CurrentCorruption += corruptionPerSecond * Time.deltaTime;
            CurrentCorruption = Mathf.Min(CurrentCorruption, maxCorruption);
            OnCorruptionChanged?.Invoke(CorruptionNormalized);

            if (IsFullyCorrupted && !gameOverTriggered)
            {
                gameOverTriggered = true;
                OnFullCorruption?.Invoke();
                // Force mask off - the girl has become a monster
                SetMaskOff();
            }
        }
    }

    private void ToggleMask()
    {
        if (IsMaskOn)
            SetMaskOff();
        else
            SetMaskOn();
    }

    private void SetMaskOn()
    {
        IsMaskOn = true;

        // Reset detection when entering spirit world
        if (DetectionBar.Instance != null)
            DetectionBar.Instance.ResetDetection();

        OnMaskOn?.Invoke();
    }

    private void SetMaskOff()
    {
        IsMaskOn = false;
        OnMaskOff?.Invoke();
    }
}