using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float pushSpeedMultiplier = 0.5f; // Push sırasında hız çarpanı

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private PlayerInteraction playerInteraction;
    private PlayerAnimation playerAnimation;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerAnimation = GetComponent<PlayerAnimation>();
    }

    void FixedUpdate()
    {
        // Game over ise hareket etme
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        float currentSpeed = moveSpeed;

        // Push sırasında yavaşla
        if (playerInteraction != null && playerInteraction.IsPushing)
        {
            currentSpeed *= pushSpeedMultiplier;
        }

        rb.linearVelocity = moveInput * currentSpeed;
        
        // Animasyon script'ine hareket bilgisini gönder
        if (playerAnimation != null)
        {
            playerAnimation.MoveInput = moveInput;
        }
    }

    public void OnMove(InputValue value)
    {
        if (GetComponent<PlayerClimb>()?.IsClimbing == true) return;
        moveInput = value.Get<Vector2>();
    }
}
