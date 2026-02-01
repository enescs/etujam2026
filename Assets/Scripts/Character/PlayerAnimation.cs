using UnityEngine;

public enum FacingDirection { Right, Left, Up, Down }

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("=== NORMAL FORM (Gerçek Dünya) ===")]
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

    [Header("=== SPIRIT FORM (Ruhlar Alemi) ===")]
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

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    private float frameTimer;
    private FacingDirection currentDirection = FacingDirection.Down;
    private bool isInSpiritWorld = false;

    public Vector2 MoveInput { get; set; }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // MaskSystem eventlerine abone ol
        if (MaskSystem.Instance != null)
        {
            MaskSystem.Instance.OnMaskOn += OnEnterSpiritWorld;
            MaskSystem.Instance.OnMaskOff += OnExitSpiritWorld;
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
        bool isMoving = MoveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            UpdateFacing();
            AnimateWalk();
        }
        else
        {
            ShowIdleSprite();
            frameTimer = 0f;
            currentFrame = 0;
        }
    }

    private void UpdateFacing()
    {
        if (Mathf.Abs(MoveInput.x) > Mathf.Abs(MoveInput.y))
        {
            currentDirection = MoveInput.x > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            currentDirection = MoveInput.y > 0 ? FacingDirection.Up : FacingDirection.Down;
        }
    }

    private Sprite[] GetCurrentWalkSprites()
    {
        if (isInSpiritWorld)
        {
            return currentDirection switch
            {
                FacingDirection.Right => spiritWalkRight,
                FacingDirection.Left => spiritWalkLeft,
                FacingDirection.Up => spiritWalkUp,
                FacingDirection.Down => spiritWalkDown,
                _ => spiritWalkDown
            };
        }
        else
        {
            return currentDirection switch
            {
                FacingDirection.Right => normalWalkRight,
                FacingDirection.Left => normalWalkLeft,
                FacingDirection.Up => normalWalkUp,
                FacingDirection.Down => normalWalkDown,
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
                FacingDirection.Right => spiritIdleRight,
                FacingDirection.Left => spiritIdleLeft,
                FacingDirection.Up => spiritIdleUp,
                FacingDirection.Down => spiritIdleDown,
                _ => spiritIdleDown
            };
        }
        else
        {
            return currentDirection switch
            {
                FacingDirection.Right => normalIdleRight,
                FacingDirection.Left => normalIdleLeft,
                FacingDirection.Up => normalIdleUp,
                FacingDirection.Down => normalIdleDown,
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
