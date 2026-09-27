using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector2 moveOffset = new Vector2(0f, 2.5f);
    public float speed = 1.5f;

    private Rigidbody2D rb;
    private Vector2 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
    }

    private void FixedUpdate()
    {
        float movement =
            (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;

        Vector2 target =
            startPosition + moveOffset * movement;

        rb.MovePosition(target);
    }
}