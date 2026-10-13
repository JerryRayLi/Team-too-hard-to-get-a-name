using UnityEngine;

public class FlashingPlatform : MonoBehaviour
{
    [Header("Flashing")]
    [Min(0.1f)] public float warningDuration = 2f;
    [Min(0.01f)] public float startingFlashInterval = 0.35f;
    [Min(0.01f)] public float fastestFlashInterval = 0.06f;

    [Header("Trap")]
    public bool collapseAtEnd = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D platformCollider;
    private bool isFlashing;
    private bool playerOnPlatform;
    private bool hasTriggered;
    private float elapsedTime;
    private float flashTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null || platformCollider == null)
        {
            Debug.LogError("FlashingPlatform requires a SpriteRenderer and Collider2D.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!isFlashing)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= warningDuration)
        {
            FinishWarning();
            return;
        }

        float progress = elapsedTime / Mathf.Max(warningDuration, 0.01f);
        float interval = Mathf.Lerp(
            startingFlashInterval,
            fastestFlashInterval,
            progress
        );

        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0f)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            flashTimer = Mathf.Max(interval, 0.01f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasTriggered ||
            collision.gameObject.GetComponent<PlayerController>() == null ||
            collision.transform.position.y <= platformCollider.bounds.center.y)
        {
            return;
        }

        playerOnPlatform = true;
        hasTriggered = true;
        isFlashing = true;
        elapsedTime = 0f;
        flashTimer = Mathf.Max(startingFlashInterval, 0.01f);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>() == null)
        {
            return;
        }

        playerOnPlatform = false;

        if (!isFlashing && !collapseAtEnd)
        {
            hasTriggered = false;
        }
    }

    private void OnDisable()
    {
        if (isFlashing && spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void FinishWarning()
    {
        isFlashing = false;
        spriteRenderer.enabled = !collapseAtEnd;
        platformCollider.enabled = !collapseAtEnd;

        if (!collapseAtEnd && !playerOnPlatform)
        {
            hasTriggered = false;
        }
    }
}
