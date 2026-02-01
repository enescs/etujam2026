using UnityEngine;

public enum EnemyFacingDirection { Right, Left, Up, Down }

/// <summary>
/// Düşman animasyon sistemi.
/// Her düşman tipi için normal dünya ve ruhlar alemi sprite'ları ayrı tanımlanır.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyAnimation : MonoBehaviour
{
    [Header("=== NORMAL DÜNYA (Monster Görünümü) ===")]
    [Header("Normal - Walk Sprites")]
    [SerializeField] private Sprite[] normalWalkRight;
    [SerializeField] private Sprite[] normalWalkLeft;
    [SerializeField] private Sprite[] normalWalkUp;
    [SerializeField] private Sprite[] normalWalkDown;
    
    [Header("Normal - Idle Sprites")]
    [SerializeField] private Sprite normalIdleRight;
    [SerializeField] private Sprite normalIdleLeft;
    [SerializeField] private Sprite normalIdleUp;
    [SerializeField] private Sprite normalIdleDown;
    
    [Header("=== RUHLAR ALEMİ (İnsan Görünümü) ===")]
    [Header("Spirit - Walk Sprites")]
    [SerializeField] private Sprite[] spiritWalkRight;
    [SerializeField] private Sprite[] spiritWalkLeft;
    [SerializeField] private Sprite[] spiritWalkUp;
    [SerializeField] private Sprite[] spiritWalkDown;
    
    [Header("Spirit - Idle Sprites")]
    [SerializeField] private Sprite spiritIdleRight;
    [SerializeField] private Sprite spiritIdleLeft;
    [SerializeField] private Sprite spiritIdleUp;
    [SerializeField] private Sprite spiritIdleDown;
    
    [Header("Animation Settings")]
    [SerializeField] private float frameRate = 8f;
    [SerializeField] private float movementThreshold = 0.01f;
    
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private int currentFrame;
    private float frameTimer;
    private EnemyFacingDirection currentDirection = EnemyFacingDirection.Down;
    private bool isInSpiritWorld = false;
    private Vector2 lastPosition;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        lastPosition = transform.position;
        
        // MaskSystem eventlerine abone ol
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn += OnEnterSpiritWorld;
            MaskSystem.Instance.OnMaskOff += OnExitSpiritWorld;
            
            // Mevcut durumu al
            isInSpiritWorld = MaskSystem.Instance.IsMaskOn;
        }
    }

    void OnDestroy()
    {
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn -= OnEnterSpiritWorld;
            MaskSystem.Instance.OnMaskOff -= OnExitSpiritWorld;
        }
    }

    private void OnEnterSpiritWorld()
    {
        isInSpiritWorld = true;
        currentFrame = 0;
        frameTimer = 0f;
    }

    private void OnExitSpiritWorld()
    {
        isInSpiritWorld = false;
        currentFrame = 0;
        frameTimer = 0f;
    }

    void Update()
    {
        // Hareket yönünü hesapla
        Vector2 currentPos = transform.position;
        Vector2 movement = currentPos - lastPosition;
        bool isMoving = movement.sqrMagnitude > movementThreshold * movementThreshold * Time.deltaTime * Time.deltaTime;
        
        if (isMoving)
        {
            UpdateFacing(movement);
            AnimateWalk();
        }
        else
        {
            ShowIdleSprite();
            frameTimer = 0f;
            currentFrame = 0;
        }
        
        lastPosition = currentPos;
    }

    private void UpdateFacing(Vector2 movement)
    {
        if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
        {
            currentDirection = movement.x > 0 ? EnemyFacingDirection.Right : EnemyFacingDirection.Left;
        }
        else
        {
            currentDirection = movement.y > 0 ? EnemyFacingDirection.Up : EnemyFacingDirection.Down;
        }
    }

    private Sprite[] GetCurrentWalkSprites()
    {
        if (isInSpiritWorld)
        {
            return currentDirection switch
            {
                EnemyFacingDirection.Right => spiritWalkRight,
                EnemyFacingDirection.Left => spiritWalkLeft,
                EnemyFacingDirection.Up => spiritWalkUp,
                EnemyFacingDirection.Down => spiritWalkDown,
                _ => spiritWalkDown
            };
        }
        else
        {
            return currentDirection switch
            {
                EnemyFacingDirection.Right => normalWalkRight,
                EnemyFacingDirection.Left => normalWalkLeft,
                EnemyFacingDirection.Up => normalWalkUp,
                EnemyFacingDirection.Down => normalWalkDown,
                _ => normalWalkDown
            };
        }
    }

    private Sprite GetCurrentIdleSprite()
    {
        if (isInSpiritWorld)
        {
            return currentDirection switch
            {
                EnemyFacingDirection.Right => spiritIdleRight,
                EnemyFacingDirection.Left => spiritIdleLeft,
                EnemyFacingDirection.Up => spiritIdleUp,
                EnemyFacingDirection.Down => spiritIdleDown,
                _ => spiritIdleDown
            };
        }
        else
        {
            return currentDirection switch
            {
                EnemyFacingDirection.Right => normalIdleRight,
                EnemyFacingDirection.Left => normalIdleLeft,
                EnemyFacingDirection.Up => normalIdleUp,
                EnemyFacingDirection.Down => normalIdleDown,
                _ => normalIdleDown
            };
        }
    }

    private void AnimateWalk()
    {
        Sprite[] activeSprites = GetCurrentWalkSprites();
        
        if (activeSprites == null || activeSprites.Length == 0) return;
        
        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        
        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % activeSprites.Length;
            spriteRenderer.sprite = activeSprites[currentFrame];
        }
    }

    private void ShowIdleSprite()
    {
        Sprite idle = GetCurrentIdleSprite();
        
        if (idle != null)
        {
            spriteRenderer.sprite = idle;
        }
        else
        {
            Sprite[] walkSprites = GetCurrentWalkSprites();
            if (walkSprites != null && walkSprites.Length > 0)
                spriteRenderer.sprite = walkSprites[0];
        }
    }
}
