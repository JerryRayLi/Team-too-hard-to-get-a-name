using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class HiddenPlatform : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool revealed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Hide the visual while keeping the collider active.
        spriteRenderer.enabled = false;
        revealed = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryReveal(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryReveal(collision);
    }

    private void TryReveal(Collision2D collision)
    {
        if (revealed ||
            collision.gameObject.GetComponent<PlayerController>() == null)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            // On the platform side, a top contact has a downward normal.
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                revealed = true;
                spriteRenderer.enabled = true;
                return;
            }
        }
    }
}
