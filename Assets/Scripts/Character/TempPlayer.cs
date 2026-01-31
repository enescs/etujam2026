using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary placeholder player controller for testing enemy AI.
/// Replace with the real player script when ready.
/// </summary>
public class TempPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 input;

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1f;
        if (kb.sKey.isPressed) input.y -= 1f;
        if (kb.dKey.isPressed) input.x += 1f;
        if (kb.aKey.isPressed) input.x -= 1f;
        input = input.normalized;

        // Flip sprite based on horizontal direction
        if (Mathf.Abs(input.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (input.x > 0f ? 1f : -1f);
            transform.localScale = scale;
        }

        Vector3 move = new Vector3(input.x, input.y, 0f);
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}