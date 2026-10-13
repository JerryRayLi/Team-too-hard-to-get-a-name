using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class FinalLevelPlayerController : MonoBehaviour
{
    public float normalMoveSpeed = 4f;
    public float normalJumpForce = 8f;
    public float normalGravityScale = 1.5f;
    public LayerMask groundMask = 1 << 8;
    public bool IsWeighted { get; private set; }
    public float CurrentSpeed => normalMoveSpeed * (IsWeighted ? 0.5f : 1f);
    public Rigidbody2D Body { get; private set; }
    public bool Grounded { get; private set; }
    float direction, jumpUntil = -1f;
    BoxCollider2D shape;
    void Awake() { Body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>(); ResetMovementState(); }
    void Update()
    {
        var k = Keyboard.current;
        direction = k == null ? 0 : ((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0));
        if (k != null && k.spaceKey.wasPressedThisFrame) jumpUntil = Time.time + 0.12f;
    }
    void FixedUpdate()
    {
        if (!FinalLevelManager.Instance || !FinalLevelManager.Instance.CanMove) { Body.linearVelocity = Vector2.zero; return; }
        var b = shape.bounds;
        var hit = Physics2D.BoxCast(new Vector2(b.center.x, b.min.y + 0.025f), new Vector2(b.size.x * 0.8f, 0.02f), 0, Vector2.down, 0.08f, groundMask);
        Grounded = hit.collider && hit.normal.y > 0.65f && Body.linearVelocity.y <= 0.1f;
        float vy = Body.linearVelocity.y;
        if (Time.time <= jumpUntil && Grounded) { vy = normalJumpForce; jumpUntil = -1; Grounded = false; }
        Body.linearVelocity = new Vector2(direction * CurrentSpeed, vy);
    }
    public void ApplyWeightedState(float speedMultiplier = 0.5f, float gravityMultiplier = 1.5f)
    {
        if (IsWeighted) return;
        IsWeighted = true;
        Body.gravityScale = normalGravityScale * 1.5f;
    }
    public void ResetMovementState()
    {
        IsWeighted = false;
        if (!Body) Body = GetComponent<Rigidbody2D>();
        Body.gravityScale = normalGravityScale;
        ResetVelocity();
    }
    public void ResetVelocity() { Body.linearVelocity = Vector2.zero; Body.angularVelocity = 0; direction = 0; jumpUntil = -1; Grounded = false; }
}

