using UnityEngine;

public class Door : MonoBehaviour
{
    private Collider2D doorCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Open()
    {
        doorCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    public void Close()
    {
        doorCollider.enabled = true;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }
}