using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ChallengeCollapsePlatform : MonoBehaviour
{
    [Min(0.1f)] public float collapseDelay = 0.8f;
    [Min(0.1f)] public float returnDelay = 2f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D platformCollider;
    private Color originalColor;
    private bool originalColliderEnabled;
    private bool originalSpriteEnabled;
    private bool triggered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<BoxCollider2D>();
        originalColor = spriteRenderer.color;
        originalColliderEnabled = platformCollider.enabled;
        originalSpriteEnabled = spriteRenderer.enabled;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTrigger(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTrigger(collision);
    }

    private void TryTrigger(Collision2D collision)
    {
        if (triggered || collision.gameObject.GetComponentInParent<PlayerController>() == null)
            return;

        // Only stepping on top triggers the trap; touching its side does not.
        if (collision.collider.bounds.min.y < platformCollider.bounds.max.y - 0.15f)
            return;

        triggered = true;
        StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        float elapsed = 0f;
        while (elapsed < collapseDelay)
        {
            elapsed += Time.deltaTime;
            float flash = Mathf.PingPong(elapsed * 9f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, Color.red, flash);
            yield return null;
        }

        spriteRenderer.enabled = false;
        platformCollider.enabled = false;
        yield return new WaitForSeconds(returnDelay);
        Restore();
    }

    private void OnDisable()
    {
        // A color switch can deactivate the object in the middle of a collapse.
        StopAllCoroutines();
        Restore();
    }

    private void Restore()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = originalSpriteEnabled;
            spriteRenderer.color = originalColor;
        }
        if (platformCollider != null)
            platformCollider.enabled = originalColliderEnabled;
        triggered = false;
    }
}
