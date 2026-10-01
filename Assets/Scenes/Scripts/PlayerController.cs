using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private MovingPlatform currentPlatform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float direction = 0f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            direction = -1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            direction = 1f;
        }

        float platformSpeed = currentPlatform != null
            ? currentPlatform.Velocity.x
            : 0f;

        rb.linearVelocity = new Vector2(
            direction * moveSpeed + platformSpeed,
            rb.linearVelocity.y
        );

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            isGrounded = false;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                currentPlatform = collision.gameObject.GetComponent<MovingPlatform>();
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;

        if (collision.gameObject.GetComponent<MovingPlatform>() == currentPlatform)
        {
            currentPlatform = null;
        }
    }
}
