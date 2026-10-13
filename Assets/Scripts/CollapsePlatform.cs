using System.Collections;
using UnityEngine;

public class CollapsePlatform : MonoBehaviour
{
    public float collapseDelay = 0.4f;
    public float returnDelay = 1.5f;

    private SpriteRenderer spriteRenderer;
    private Collider2D platformCollider;
    private bool triggered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered)
        {
            return;
        }

        if (collision.gameObject.GetComponent<PlayerController>() != null)
        {
            StartCoroutine(Collapse());
        }
    }

    private IEnumerator Collapse()
    {
        triggered = true;

        yield return new WaitForSeconds(collapseDelay);

        spriteRenderer.enabled = false;
        platformCollider.enabled = false;

        yield return new WaitForSeconds(returnDelay);

        spriteRenderer.enabled = true;
        platformCollider.enabled = true;
        triggered = false;
    }
}