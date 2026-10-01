using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public enum MoveDirection
    {
        Vertical,
        Horizontal
    }

    public MoveDirection direction = MoveDirection.Vertical;
    public float distance = 2.5f;
    public float speed = 1.5f;

    public Vector2 Velocity { get; private set; }

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

        Vector2 moveOffset = direction == MoveDirection.Horizontal
            ? Vector2.right * distance
            : Vector2.up * distance;

        Vector2 target = startPosition + moveOffset * movement;

        Velocity = (target - rb.position) / Time.fixedDeltaTime;

        rb.MovePosition(target);
    }
}
