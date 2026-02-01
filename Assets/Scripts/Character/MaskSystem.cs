using UnityEngine;
using System;
using System.Collections;

public class MaskSystem : MonoBehaviour
{
    public static MaskSystem Instance { get; private set; }

    [Header("Mask Duration Settings")]
    [SerializeField] private float maxMaskDuration = 90f; // 90 saniye
    [SerializeField] private float flashDuration = 1.5f; // Ölmeden önce yanıp sönme süresi
    [SerializeField] private float flashInterval = 0.1f; // Yanıp sönme hızı

    [Header("Corruption Settings")]
    [SerializeField] private float maxCorruption = 100f;
    [SerializeField] private float corruptionPerSecond = 5f; // while mask is on
    [SerializeField] private float corruptionThresholdGameOver = 100f;

    public bool IsMaskOn { get; private set; }
    public float CurrentCorruption { get; private set; }
    public float CorruptionNormalized => CurrentCorruption / maxCorruption;
    public bool IsFullyCorrupted => CurrentCorruption >= corruptionThresholdGameOver;

    // Mask duration
    public float RemainingMaskDuration { get; private set; }
    public float MaskDurationNormalized => RemainingMaskDuration / maxMaskDuration;
    public bool IsMaskDepleted => RemainingMaskDuration <= 0f;

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

    /// <summary>
    /// Fired each frame with normalized mask duration for UI.
    /// </summary>
    public event Action<float> OnMaskDurationChanged;

    /// <summary>
    /// Fired when mask duration reaches zero.
    /// </summary>
    public event Action OnMaskDepleted;

    private bool gameOverTriggered;
    private bool maskDepletedTriggered;
    private SpriteRenderer playerSpriteRenderer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Mask süresini başlat
        RemainingMaskDuration = maxMaskDuration;

        // Player'ın SpriteRenderer'ını bul (flash için)
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        // Mask takılıyken süreyi azalt
        if (IsMaskOn && !maskDepletedTriggered)
        {
            RemainingMaskDuration -= Time.deltaTime;
            RemainingMaskDuration = Mathf.Max(RemainingMaskDuration, 0f);
            OnMaskDurationChanged?.Invoke(MaskDurationNormalized);

            // Süre bittiyse - yanıp sön ve öl
            if (IsMaskDepleted)
            {
                maskDepletedTriggered = true;
                Debug.Log("[MaskSystem] Mask duration depleted! Player dying...");
                OnMaskDepleted?.Invoke();
                StartCoroutine(FlashAndDie());
            }
        }

        // Accumulate corruption while mask is on (eski sistem - şimdilik devre dışı)
        // if (IsMaskOn)
        // {
        //     CurrentCorruption += corruptionPerSecond * Time.deltaTime;
        //     CurrentCorruption = Mathf.Min(CurrentCorruption, maxCorruption);
        //     OnCorruptionChanged?.Invoke(CorruptionNormalized);
        //
        //     if (IsFullyCorrupted && !gameOverTriggered)
        //     {
        //         gameOverTriggered = true;
        //         OnFullCorruption?.Invoke();
        //         SetMaskOff();
        //     }
        // }
    }

    public void ToggleMask()
    {
        // Süre bittiyse maske açılamaz
        if (maskDepletedTriggered)
        {
            Debug.Log("[MaskSystem] Cannot toggle mask - depleted!");
            return;
        }

        // Süre kalmadıysa maske açılamaz
        if (!IsMaskOn && RemainingMaskDuration <= 0f)
        {
            Debug.Log("[MaskSystem] Cannot turn mask on - no duration left!");
            return;
        }

        if (IsMaskOn)
            SetMaskOff();
        else
            SetMaskOn();
    }

    private void SetMaskOn()
    {
        IsMaskOn = true;
        Debug.Log("[MaskSystem] Mask ON - Spirit World");

        // Reset detection when entering spirit world
        if (DetectionBar.Instance != null)
            DetectionBar.Instance.ResetDetection();

        OnMaskOn?.Invoke();
    }

    private void SetMaskOff()
    {
        IsMaskOn = false;
        Debug.Log("[MaskSystem] Mask OFF - Real World");
        OnMaskOff?.Invoke();
    }

    private IEnumerator FlashAndDie()
    {
        // Oyuncu kontrolünü kapat
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Yanıp sönme efekti
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < flashDuration)
        {
            elapsed += flashInterval;

            if (playerSpriteRenderer != null)
            {
                visible = !visible;
                playerSpriteRenderer.enabled = visible;
            }

            yield return new WaitForSeconds(flashInterval);
        }

        // Sprite'ı tekrar göster
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.enabled = true;

        // Game Over
        Debug.Log("[MaskSystem] Mask depleted - GAME OVER!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}